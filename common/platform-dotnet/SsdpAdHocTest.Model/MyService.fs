namespace SsdpAdHocTest.Model

module MyService =
    open SoundMetrics.Network

    let buildSsdpService serviceType usn body multicastLoopback =

        let svcs = [
            {
                ServiceType = serviceType
                Server = "windows/soundmetrics-com/MyService"
                UniqueServerName = usn
                MimeType = "text/x.kvp"
                IsActive = fun () -> true
                GetServiceBodyText = fun () -> body
            }
        ]

        new SsdpService("MyService", svcs, multicastLoopback)


