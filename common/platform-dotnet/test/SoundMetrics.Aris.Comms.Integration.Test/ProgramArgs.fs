module ProgramArgs

open Argu

type ProgramArgs =
    | [<AltCommandLine("-l")>]  List_Tests
    | [<AltCommandLine("-a")>]  All
    | [<AltCommandLine("-sn")>] Serial_Number of uint32
    | [<AltCommandLine("-v")>]  Verbose
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | List_Tests _ ->       "lists the tests available by name."
            | All _ ->              "run all tests."
            | Serial_Number _ ->    "specifies the serial number of the sonar to use for testing."
            | Verbose _ ->          "enables verbose output."
