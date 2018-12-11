namespace SsdpAdHocTest.Model

module MyService =
    open SoundMetrics.Network
    open System

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

        let announcementPeriod = TimeSpan.FromSeconds(5.0)
        new SsdpService("MyService", svcs, announcementPeriod, multicastLoopback)


