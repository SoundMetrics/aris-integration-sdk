/* connect */

#include "availability.pb-c.h"
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

uint8_t beacon_buf[MAX_BEACON_SIZE];
uint8_t command_buf[MAX_COMMAND_SIZE];

int validate_inputs(int argc, char** argv,
                    unsigned int* serial);
int start_listening(SOCKET* listener_socket);
int find_sonar(SOCKET beacon_socket, uint32_t serial,
               Aris__Availability__SystemType* system_type,
               SOCKADDR* sonar_address);
int connect_to_sonar(SOCKET* command_socket,
                     Aris__Availability__SystemType system_type,
                     SOCKADDR* sonar_address);
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
            break;
        }
    }

    return 0;
}

void show_availability(Aris__Availability* msg) {

    if (msg->has_serialnumber && msg->has_systemtype && msg->has_connectionstate) {
        int32_t freq = 1800;

        if (msg->systemtype != ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_1800) {
           freq = (msg->systemtype == ARIS__AVAILABILITY__SYSTEM_TYPE__ARIS_1200) ? 1200 : 3000;
        }

        fprintf(stdout, "ARIS %u serial=%u ", freq, msg->serialnumber);
        fprintf(stdout, "is %s.\n", msg->connectionstate  ? "busy" : "available");
        fflush(stdout);
    }
}

int connect_to_sonar(SOCKET* command_socket,
                     Aris__Availability__SystemType system_type, 
                     SOCKADDR* sonar_address) {

    // TODO
    return 0;
}

