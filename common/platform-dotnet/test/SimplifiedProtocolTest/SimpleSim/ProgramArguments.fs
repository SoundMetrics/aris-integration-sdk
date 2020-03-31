namespace SimpleSim

open Argu

type Arguments = {
    SerialNumber:   string
}

type ArgumentsOrMessage =
    | Arguments of args: Arguments
    | Message of message: string
    | ShowUsage

type ProgramArguments =
    | [<Mandatory; Unique>] Serial_Number of serialNumber: string
with
    interface IArgParserTemplate with
        member self.Usage =
            match self with| Serial_Number _ -> "ARIS serial number"

    static member GetProgramArguments argv : ArgumentsOrMessage =

        let parser = ArgumentParser.Create<ProgramArguments>(programName = "simplesim.exe")

        try
            let args = parser.Parse(argv)

            if args.IsUsageRequested then
                ShowUsage
            else
                let all = args.GetAllResults()

                let expectArgCount = 1

                if expectArgCount <> 1 then
                    Message "Wrong number of parameters"
                else
                    match all with
                    | Serial_Number sn :: _ -> Arguments { SerialNumber = sn }
                    | _ -> Message "Unhandled argument type"
        with
            | :? ArguParseException as ex -> Message ex.Message
