# About Protocol Buffers for ARIS

## protobuf v3

The ARIS command protocols originally used Google Protocol Buffers version 2, and had a few required fields.
Protobuf version 3 is supposed to be compatible, but is not in cases where the field was formerly required
(`required` is no longer supported) and the value is zero or "nothing"--in this case, the field is "optimized out" of the payload.
The client implementor should be aware that all fields must now be considered optional and their presence checked.
Not checking may lead to a null reference exception.
