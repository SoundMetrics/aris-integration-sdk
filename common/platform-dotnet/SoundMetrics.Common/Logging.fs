// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open Serilog.Context
open System

module Logging =

    let private ModuleKey = "Module"

    let pushModuleName (prefix: string) (memberName: string) : IDisposable =

        LogContext.PushProperty(ModuleKey, prefix + memberName)
