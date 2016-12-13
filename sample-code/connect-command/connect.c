/* connect */

#include "availability.pb-c.h"
#include "commands.pb-c.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <winsock2.h>
#include <Ws2tcpip.h>

#define INVALID_INPUTS          -1
#define CANT_START_WINSOCK      -2
#define CANT_START_LISTENER     -3
#define CANT_FIND_SONAR         -4
#define CANT_CONNECT_TO_SONAR   -5

#define MAX_BEACON_SIZE     256 
#define MAX_COMMAND_SIZE    1024
#define AVAILABILITY_PORT   56124
#define COMMAND_PORT        56888 

#define ARIS_1200   ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_1200
#define ARIS_1800   ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_1800
#define ARIS_3000   ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_3000

#define HIGH_FREQ   ARIS__COMMAND__SET_ACOUSTIC_SETTINGS__FREQUENCY__HIGH

uint8_t beacon_buf[MAX_BEACON_SIZE];
uint8_t command_buf[MAX_COMMAND_SIZE];

typedef struct {
    Aris__Command__SetAcousticSettings__Frequency frequency;
    uint32_t ping_mode;
    uint32_t samples_per_beam;
    uint32_t sample_period;
    uint32_t sample_start_delay;
    uint32_t cycle_period;
    uint32_t pulse_width;
    float frame_rate;
} acoustic_settings;

acoustic_settings sonar_settings[3] = {
   { HIGH_FREQ, 3, 1000, 4, 1360,  5720, 6, 12.0f },  /* ARIS 1800 */
   { HIGH_FREQ, 9,  800, 4, 1360,  4920, 6, 12.0f },  /* ARIS 3000 */
   { HIGH_FREQ, 1,  512, 4, 1333, 39820, 5,  8.0f }   /* ARIS 1200 */
};

int validate_inputs(int argc, char** argv,
                    unsigned int* serial);
int start_listening(SOCKET* listener_socket);
int find_sonar(SOCKET beacon_socket, uint32_t serial,
               Aris__Availability__SystemType* system_type,
               SOCKADDR* sonar_address);
int connect_to_sonar(SOCKET* command_socket,
                     Aris__Availability__SystemType system_type,
                     SOCKADDR* sonar_address);
int send_sonar_settings(SOCKET command_socket,
                        Aris__Availability__SystemType system_type);
int32_t read_length_prefix(SOCKET socket, SOCKADDR* server_address);
void show_usage(void);
void show_availability(Aris__Availability* msg);

int main(int argc, char** argv) {

    unsigned int serial;
    Aris__Availability__SystemType system_type;
    struct sockaddr_in sonar_address;
    WSADATA wsa_data;
    SOCKET beacon_socket;
    SOCKET command_socket;

    if (validate_inputs(argc, argv, &serial)) {
       show_usage();
       return INVALID_INPUTS;
    }

    if (WSAStartup(MAKEWORD(2,2), &wsa_data) != NO_ERROR) {
       return CANT_START_WINSOCK;
    } 

    if (start_listening(&beacon_socket)) {
       return CANT_START_LISTENER; 
    }

    if (find_sonar(beacon_socket, serial, &system_type, (SOCKADDR*)&sonar_address)) {
       return CANT_FIND_SONAR;
    }

    if (connect_to_sonar(&command_socket, system_type, (SOCKADDR*)&sonar_address)) {
       return CANT_CONNECT_TO_SONAR;
    }

    /* Log beacons to console indefinitely. */
    find_sonar(beacon_socket, 0, &system_type, (SOCKADDR*)&sonar_address);

    closesocket(beacon_socket);
    closesocket(command_socket);
    WSACleanup();

    return 0;
}

void show_usage(void) {

    fprintf(stderr, "USAGE:\n");
    fprintf(stderr, "    connect <sonar serial number>");
    fprintf(stderr, "\n");
}

int validate_inputs(int argc, char** argv,
                    unsigned int* serial) {

    if (argc != 2) {
        fprintf(stderr, "Bad number of arguments.\n");
        return 1;
    }

    char * ignore;
    unsigned long int number = strtoul(argv[1], &ignore, 10);

    if (!number) {
        fprintf(stderr, "Not a serial number.\n");
        return 2;
    }

    *serial = number; 

    return 0;
}

int start_listening(SOCKET* listener_socket) {

    struct sockaddr_in recv_address;

    *listener_socket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);

    if (*listener_socket == INVALID_SOCKET) {
        fprintf(stderr, "Failed to create listener socket.\n");
        return 1;
    }

    recv_address.sin_family = AF_INET;
    recv_address.sin_port = htons(AVAILABILITY_PORT);
    recv_address.sin_addr.s_addr = htonl(INADDR_ANY);

    if (bind(*listener_socket, (SOCKADDR*)&recv_address, sizeof(recv_address)) != 0) {
        fprintf(stderr, "Failed to bind listener socket.\n");
        closesocket(*listener_socket);
        return 2;
    }

    return 0; 
}

int32_t read_length_prefix(SOCKET socket, SOCKADDR* server_address) {

    int32_t length_prefix;
    int32_t message_length;
    int address_size = sizeof(*server_address);
    int numbytes;

    numbytes = recvfrom(socket, (char *)length_prefix, 4, 0,
                        server_address, &address_size);

    if (numbytes == SOCKET_ERROR || numbytes != 4) {
        fprintf(stderr, "Read %d bytes for length prefix.\n", numbytes);
        return 0;
    }

    message_length = ntohl(length_prefix);

    if (message_length < 0) {
        fprintf(stderr, "Read length prefix less than 0.\n");
        return 0;
    }

    return message_length;
}

/* Given a serial number find the corresponding sonar's system type and IP address. */ 
int find_sonar(SOCKET beacon_socket, uint32_t serial,
               Aris__Availability__SystemType* system_type,
               SOCKADDR* sonar_address) {

    BOOL found = FALSE;
    int address_size = sizeof(*sonar_address);
    int num_bytes;
    Aris__Availability* msg; 

    while (!found) {
        /* Get sonar's IP address from incoming packet. */
        num_bytes = recvfrom(beacon_socket, beacon_buf, MAX_BEACON_SIZE, 0,
                             sonar_address, &address_size);

        if (num_bytes == SOCKET_ERROR) {
            fprintf(stderr, "An error occurred while receiving sonar beacon.\n");
            return 1;
        }

        msg = aris__availability__unpack(NULL, num_bytes, beacon_buf);

        if (msg == NULL) {
            fprintf(stderr, "Failed to unpack incoming sonar beacon.\n");
            return 2;
        }

        show_availability(msg);

        if (msg->has_serialnumber && msg->serialnumber == serial) {
            fprintf(stdout, "Found sonar serial=%u.\n", serial);
            if (msg->has_systemtype) {
                *system_type = msg->systemtype;
            }
            found = TRUE;
        }

        aris__availability__free_unpacked(msg, NULL);
    }

    return 0;
}

void show_availability(Aris__Availability* msg) {

    if (msg->has_serialnumber && msg->has_systemtype && msg->has_connectionstate) {
        uint32_t freq;

        switch (msg->systemtype) {
            case ARIS_1200:
                freq = 1200;
                break;
            case ARIS_1800:
                freq = 1800;
                break;
            case ARIS_3000:
                freq = 3000;
                break;
	    default:
                freq = 0;
                break;
        }

	if (freq) {
            fprintf(stdout, "ARIS %u serial=%u ", freq, msg->serialnumber);
            fprintf(stdout, "is %s.\n", msg->connectionstate ? "busy" : "available");
            fflush(stdout);
        } else {
            fprintf(stderr, "ARIS model unknown serial=%u ", msg->serialnumber);
            fprintf(stderr, "is %s.\n", msg->connectionstate ? "busy" : "available");
            fflush(stderr);
        }
    }
}

int connect_to_sonar(SOCKET* command_socket,
                     Aris__Availability__SystemType system_type, 
                     SOCKADDR* sonar_address) {

    *command_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

    if (*command_socket == INVALID_SOCKET) {
        fprintf(stderr, "Failed to create command socket.\n");
        return 1;
    }

    ((struct sockaddr_in*)(sonar_address))->sin_port = htons(COMMAND_PORT);

    if (connect(*command_socket, sonar_address, sizeof(*sonar_address)) == SOCKET_ERROR) {
        fprintf(stderr, "Failed to connect to sonar.\n");
        closesocket(*command_socket);
        return 2; 
    }

    return send_sonar_settings(*command_socket, system_type);
}

int send_sonar_settings(SOCKET command_socket,
                        Aris__Availability__SystemType system_type) { 

    Aris__Command command = ARIS__COMMAND__INIT;
    Aris__Command__SetAcousticSettings settings = ARIS__COMMAND__SET_ACOUSTIC_SETTINGS__INIT;
    acoustic_settings acoustics = sonar_settings[system_type];

    command.type = ARIS__COMMAND__COMMAND_TYPE__SET_ACOUSTICS;
    command.settings = &settings;

    settings.has_cookie = TRUE;
    settings.has_enabletransmit = TRUE;
    settings.has_enable150volts = TRUE;
    settings.has_receivergain = TRUE;
    settings.has_frequency = TRUE;
    settings.has_pingmode = TRUE;
    settings.has_samplesperbeam = TRUE; 
    settings.has_sampleperiod = TRUE;
    settings.has_samplestartdelay = TRUE;
    settings.has_cycleperiod = TRUE;
    settings.has_pulsewidth = TRUE;
    settings.has_framerate = TRUE;

    /* Encode length prefix. */
    /* FIXME: Why is packed size short a variable number of bytes? */
    const int32_t missing_bytes = (system_type == ARIS_1200) ? 4 : 3;
    const int32_t packed_size = aris__command__get_packed_size(&command);
    const int32_t message_length = missing_bytes + packed_size;
    const int32_t length_prefix = htonl(message_length);
    const int32_t total_length = sizeof(length_prefix) + message_length;
    memcpy(command_buf, (void *)&length_prefix, sizeof(length_prefix));

    /* Increment cookie every time settings are sent during a session. */
    settings.cookie = 1;

    /* fixed */
    settings.enabletransmit = TRUE;
    settings.enable150volts = TRUE;
    settings.receivergain = 12.0f;

    /* system or range-dependent */
    settings.frequency = acoustics.frequency;
    settings.pingmode = acoustics.ping_mode;
    settings.samplesperbeam = acoustics.samples_per_beam;
    settings.sampleperiod = acoustics.sample_period;
    settings.samplestartdelay = acoustics.sample_start_delay;
    settings.cycleperiod = acoustics.cycle_period;
    settings.pulsewidth = acoustics.pulse_width;
    settings.framerate = acoustics.frame_rate;

    aris__command__pack(&command, &command_buf[4]);
    const int numbytes = send(command_socket, command_buf, total_length, 0);

    if (numbytes == SOCKET_ERROR || numbytes < total_length) { 
        fprintf(stderr, "Failed to send sonar settings.  ");
        fprintf(stderr, "%d of %d bytes sent\n", numbytes, total_length);	
        return 3;
    }

    return 0;
}

