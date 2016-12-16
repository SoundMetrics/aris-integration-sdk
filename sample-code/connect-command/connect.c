/* connect */

/* pb-c.h and pb-c.c files are generated by protoc-c tool. */
#include "availability.pb-c.h"
#include "commands.pb-c.h"
#include "frame_stream.pb-c.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <winsock2.h>
#include <Ws2tcpip.h>

#define INVALID_INPUTS          -1
#define CANT_START_WINSOCK      -2
#define CANT_RECEIVE_BEACONS    -3
#define CANT_FIND_SONAR         -4
#define CANT_CONNECT_TO_SONAR   -5
#define CANT_RECEIVE_FRAMES     -6

#define MAX_BEACON_SIZE     256 
#define MAX_COMMAND_SIZE    1024
#define MAX_DATAGRAM_SIZE   1400 
#define AVAILABILITY_PORT   56124
#define COMMAND_PORT        56888 
#define FRAME_STREAM_PORT   56444 
#define TOTAL_FRAME_PARTS   2200 

/* Substitute shorter names for protocol symbols to improve readability. */
#define ARIS_1200   ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_1200
#define ARIS_1800   ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_1800
#define ARIS_3000   ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_3000
#define HIGH_FREQ   ARIS__COMMAND__SET_ACOUSTIC_SETTINGS__FREQUENCY__HIGH

uint8_t beacon_buf[MAX_BEACON_SIZE];
uint8_t command_buf[MAX_COMMAND_SIZE];
uint8_t frame_buf[MAX_DATAGRAM_SIZE];

/* See the Integration SDK documentation for information on how to determine
 * valid acoustic settings.
 * Your application's acoustic settings may be either static or dynamically
 * determined based on your needs.
 */ 
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
int start_listening(SOCKET* listener_socket, uint16_t port);
int find_sonar(SOCKET beacon_socket, uint32_t serial,
               Aris__Availability__SystemType* system_type,
               SOCKADDR* sonar_address);
int connect_to_sonar(SOCKET* command_socket,
                     Aris__Availability__SystemType system_type,
                     SOCKADDR* sonar_address);
int send_command(SOCKET command_socket, Aris__Command* command);
int send_sonar_settings(SOCKET command_socket,
                        Aris__Availability__SystemType system_type);
int send_frame_stream_receiver(SOCKET command_socket, uint16_t port);
int receive_frame_part(SOCKET frame_stream_socket); 
void show_usage(void);
void show_availability(Aris__Availability* beacon);
void show_frame_part(FrameStream__FramePart* frame_part);

int main(int argc, char** argv) {

    unsigned int serial;
    Aris__Availability__SystemType system_type;
    struct sockaddr_in sonar_address;
    WSADATA wsa_data;
    SOCKET beacon_socket;
    SOCKET command_socket;
    SOCKET frame_stream_socket;

    if (validate_inputs(argc, argv, &serial)) {
        show_usage();
        return INVALID_INPUTS;
    }

    if (WSAStartup(MAKEWORD(2,2), &wsa_data) != NO_ERROR) {
        return CANT_START_WINSOCK;
    } 

    if (start_listening(&beacon_socket, AVAILABILITY_PORT)) {
        return CANT_RECEIVE_BEACONS; 
    }

    if (find_sonar(beacon_socket, serial, &system_type, (SOCKADDR*)&sonar_address)) {
        return CANT_FIND_SONAR;
    }

    if (connect_to_sonar(&command_socket, system_type, (SOCKADDR*)&sonar_address)) {
        return CANT_CONNECT_TO_SONAR;
    }

    if (start_listening(&frame_stream_socket, FRAME_STREAM_PORT)) {
        return CANT_RECEIVE_FRAMES;
    }

    for (int32_t frame_part_count = 0;
         frame_part_count < TOTAL_FRAME_PARTS; 
         ++frame_part_count) {
	    if (receive_frame_part(frame_stream_socket)) {
            break;
        }
    }

    closesocket(beacon_socket);
    closesocket(command_socket);
    closesocket(frame_stream_socket);
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

    char* ignore;
    unsigned long int number = strtoul(argv[1], &ignore, 10);

    if (!number) {
        fprintf(stderr, "Not a serial number.\n");
        return 2;
    }

    *serial = number; 

    return 0;
}

int start_listening(SOCKET* listener_socket, uint16_t port) {

    struct sockaddr_in recv_address;

    *listener_socket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);

    if (*listener_socket == INVALID_SOCKET) {
        fprintf(stderr, "Failed to create listener socket.\n");
        return 1;
    }

    recv_address.sin_family = AF_INET;
    recv_address.sin_port = htons(port);
    recv_address.sin_addr.s_addr = htonl(INADDR_ANY);

    if (bind(*listener_socket, (SOCKADDR*)&recv_address, sizeof(recv_address)) != 0) {
        fprintf(stderr, "Failed to bind listener socket.\n");
        closesocket(*listener_socket);
        return 2;
    }

    return 0; 
}

/* Given a serial number find the corresponding sonar's system type and IP address. */ 
int find_sonar(SOCKET beacon_socket, uint32_t serial,
               Aris__Availability__SystemType* system_type,
               SOCKADDR* sonar_address) {

    BOOL found = FALSE;
    int address_size = sizeof(*sonar_address);
    int num_bytes;
    Aris__Availability* beacon; 

    while (!found) {
        /* Get sonar's IP address from incoming packet. */
        num_bytes = recvfrom(beacon_socket, beacon_buf, MAX_BEACON_SIZE, 0,
                             sonar_address, &address_size);

        if (num_bytes == SOCKET_ERROR) {
            fprintf(stderr, "An error occurred while receiving sonar beacon.\n");
            return 1;
        }

        beacon = aris__availability__unpack(NULL, num_bytes, beacon_buf);

        if (beacon == NULL) {
            fprintf(stderr, "Failed to unpack incoming sonar beacon.\n");
            return 2;
        }

        show_availability(beacon);

        if (beacon->has_serialnumber && beacon->serialnumber == serial) {
            fprintf(stdout, "Found ARIS serial=%u.\n", serial);
            fflush(stdout);
            if (beacon->has_systemtype) {
                *system_type = beacon->systemtype;
            }
            found = TRUE;
        }

        aris__availability__free_unpacked(beacon, NULL);
    }

    return 0;
}

void show_availability(Aris__Availability* beacon) {

    if (beacon->has_serialnumber && beacon->has_systemtype && beacon->has_connectionstate) {
        uint32_t freq;

        switch (beacon->systemtype) {
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
            fprintf(stdout, "ARIS %u serial=%u ", freq, beacon->serialnumber);
            fprintf(stdout, "is %s.\n", beacon->connectionstate ? "busy" : "available");
            fflush(stdout);
        } else {
            fprintf(stderr, "ARIS model unknown serial=%u ", beacon->serialnumber);
            fprintf(stderr, "is %s.\n", beacon->connectionstate ? "busy" : "available");
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

    if (send_sonar_settings(*command_socket, system_type)) {
        fprintf(stderr, "Failed to send sonar settings.\n");
        closesocket(*command_socket);
        return 3;
    }

    if (send_frame_stream_receiver(*command_socket, FRAME_STREAM_PORT)) {
        fprintf(stderr, "Failed to send frame stream receiver port.\n");
        closesocket(*command_socket);
        return 4;
    }
}

int send_command(SOCKET command_socket, Aris__Command* command) {

    /* Determine packed size and encode it into length prefix.
     * Packed size needs to be measured after the message is entirely defined
     * because the choice of field values impacts the serialized size.
     * Use htonl to ensure length prefix is in network byte order.
     */
    const int32_t packed_size = aris__command__get_packed_size(command);
    const int32_t length_prefix = htonl(packed_size);
    const int32_t total_length = sizeof(length_prefix) + packed_size;
    memcpy(command_buf, (void*)&length_prefix, sizeof(length_prefix));

    /* Serialize command message after length prefix in command buffer. */
    aris__command__pack(command, &command_buf[sizeof(length_prefix)]);

    const int numbytes = send(command_socket, command_buf, total_length, 0);

    if (numbytes == SOCKET_ERROR) {
        fprintf(stderr, "An error occurred while sending command.\n");
        return 1;
    } else if (numbytes < total_length) {
        fprintf(stderr, "Only %d of %d command bytes were sent.\n", numbytes, total_length);
        return 2;
    }

    return 0;
}

int send_sonar_settings(SOCKET command_socket,
                        Aris__Availability__SystemType system_type) { 

    Aris__Command command = ARIS__COMMAND__INIT;
    Aris__Command__SetAcousticSettings settings = ARIS__COMMAND__SET_ACOUSTIC_SETTINGS__INIT;
    acoustic_settings acoustics = sonar_settings[system_type];

    command.type = ARIS__COMMAND__COMMAND_TYPE__SET_ACOUSTICS;
    command.settings = &settings;

    /* C-based implementations may need to explicity set that the fields have
     * been provided (.has_*), as done here.
     * C++ (or other language) implementations may use a protobuf library that
     * does this for you.
     */ 
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

    return send_command(command_socket, &command);
}

int send_frame_stream_receiver(SOCKET command_socket, uint16_t port) {

    Aris__Command command = ARIS__COMMAND__INIT;
    Aris__Command__SetFrameStreamReceiver receiver = ARIS__COMMAND__SET_FRAME_STREAM_RECEIVER__INIT;

    command.type = ARIS__COMMAND__COMMAND_TYPE__SET_FRAMESTREAM_RECEIVER;
    command.framestreamreceiver = &receiver;

    receiver.ip = NULL;
    receiver.has_port = TRUE;
    receiver.port = port;

    return send_command(command_socket, &command);
}

int receive_frame_part(SOCKET frame_stream_socket) {

    FrameStream__FramePart* frame_part;

    int numbytes = recvfrom(frame_stream_socket, frame_buf, MAX_DATAGRAM_SIZE,
                            0, NULL, NULL);

    if (numbytes == SOCKET_ERROR) {
        fprintf(stderr, "An error occurred while receiving frame parts.\n");
        return 1;
    }

    frame_part = frame_stream__frame_part__unpack(NULL, numbytes, frame_buf);

    if (frame_part == NULL) {
        fprintf(stderr, "Failed to unpack incoming frame part.\n");
        return 2;
    }

    show_frame_part(frame_part);

    frame_stream__frame_part__free_unpacked(frame_part, NULL);

    return 0;
}

void show_frame_part(FrameStream__FramePart* frame_part) {

    /* Only first part of a frame has a header. */
    if (frame_part->has_header && frame_part->has_frame_index) {
        fprintf(stdout, "Receiving frame %d\n", frame_part->frame_index);
        fflush(stdout);
    }
}

