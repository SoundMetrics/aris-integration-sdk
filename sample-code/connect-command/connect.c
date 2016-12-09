/* connect */

#include "availability.pb-c.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <winsock2.h>
#include <Ws2tcpip.h>

#define INVALID_INPUTS        -1
#define CANT_START_WINSOCK    -2
#define CANT_START_LISTENER   -3
#define CANT_RECEIVE_BEACON   -4

#define MAX_BEACON_SIZE     256 
#define AVAILABILITY_PORT   56124

uint8_t beacon_buf[MAX_BEACON_SIZE];

int validate_inputs(int argc,
                    char** argv,
                    unsigned int* serial);
int start_listening(SOCKET* listener_socket);
int32_t read_length_prefix(SOCKET socket, SOCKADDR* server_address);
void show_usage(void);
void show_availability(uint8_t buf[], uint32_t length);

int main(int argc, char** argv) {

    unsigned int serial;
    WSADATA wsa_data;
    SOCKET beacon_socket;
    struct sockaddr_in sonar_address;
    int address_size = sizeof(sonar_address); 
    int numbytes;

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

    if ((numbytes = recvfrom(beacon_socket, beacon_buf, MAX_BEACON_SIZE, 0,
                             (SOCKADDR *)&sonar_address, &address_size)) == SOCKET_ERROR) {
       return CANT_RECEIVE_BEACON; 
    }

    show_availability(beacon_buf, numbytes);

    WSACleanup();

    return 0;
}

void show_usage(void) {

    fprintf(stderr, "USAGE:\n");
    fprintf(stderr, "    connect <sonar serial number>");
    fprintf(stderr, "\n");
}

int validate_inputs(int argc,
                    char** argv,
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

    if (bind(*listener_socket, (SOCKADDR *) & recv_address, sizeof(recv_address)) != 0) {
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

void show_availability(uint8_t buf[], uint32_t length) {

    Aris__Availability* msg;

    msg = aris__availability__unpack(NULL, length, buf);

    if (msg == NULL) {
        fprintf(stderr, "Failed to unpack incoming ARIS availability message.\n");
        return;	
    }

    if (msg->has_serialnumber && msg->has_systemtype && msg->has_connectionstate) {
        uint32_t freq = 1800;

	if (msg->systemtype) {
           freq = (msg->systemtype == 2) ? 1200 : 3000;
	}

        fprintf(stdout, "ARIS %u serial=%u ", freq, msg->serialnumber);
        fprintf(stdout, "is %s.\n", msg->connectionstate  ? "busy" : "available"); 
    }
}

