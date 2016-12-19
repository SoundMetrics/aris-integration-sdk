REORDER_DIR=..\..\..\common\code\Reordering
FILESDK_DIR=..\..\..\submodules\aris-file-sdk\type-definitions\C
INCLUDES=-I $(REORDER_DIR) -I $(FILESDK_DIR)
SOURCES=..\reorderframe.cpp $(REORDER_DIR)\Reorder.cpp $(REORDER_DIR)\Frame.cpp

reordermake: $(SOURCES)
	g++ -std=c++1y -o reorder.exe $(SOURCES) $(INCLUDES) -lstdc++
