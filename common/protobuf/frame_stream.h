// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.
// frame_stream.h

// Wrapper so we can eliminate warnings on the generated protocol buffers
// header.

#pragma warning(push)
// Avoid C4127 in MSVC: warning C4127: conditional expression is constant
// C4512: assignment operator could not be generated
// C4996: 'std::is_pod<google::protobuf::internal::ParseTableField>': warning STL4025: std::is_pod and std::is_pod_v are deprecated in C++20. The std::is_trivially_copyable and/or std::is_standard_layout traits likely suit your use case. You can define _SILENCE_CXX20_IS_POD_DEPRECATION_WARNING or _SILENCE_ALL_CXX20_DEPRECATION_WARNINGS to acknowledge that you have received this warning.
// generated_message_util.h(175,26): warning C5054: operator '*': deprecated between enumerations of different types
#pragma warning(disable: 4100 4127 4512 4996 5054)

#include "frame_stream.pb.h"

#pragma warning(pop)
