PROTOCOLS_DIR=..\..\..\common\protobuf
PROTOBUFC_DIR=..\..\..\..\protobuf-c
FILESDK_DIR=..\..\..\..\aris-file-sdk\type-definitions\C
INCLUDES=-I $(PROTOCOLS_DIR) -I $(PROTOBUFC_DIR) -I $(FILESDK_DIR)
SOURCES=..\connect.c ..\availability.pb-c.c ..\commands.pb-c.c

all: protocols connect

clean:
	rm connect.exe *.proto ../*.pb-c.*

protocols:
	cp ../../../common/protobuf/*.proto .
	protoc-c --c_out=../. *.proto

connect: $(SOURCES)
	gcc $(SOURCES) $(INCLUDES) -L/usr/lib -lws2_32 -lprotobuf-c -o connect.exe 

