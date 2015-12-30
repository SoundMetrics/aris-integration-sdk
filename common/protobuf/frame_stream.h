// Copyright (c) 2013-2014 Sound Metrics Corporation. All Rights Reserved.
// frame_stream.h

// Wrapper so we can eliminate warnings on the generated protocol buffers
// header.

#pragma warning(push)
// Avoid C4127 in MSVC: warning C4127: conditional expression is constant
// C4512: assignment operator could not be generated
#pragma warning(disable: 4100 4127 4512)

#include "frame_stream.pb.h"

#pragma warning(pop)
