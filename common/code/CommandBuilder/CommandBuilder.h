// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.

#pragma once

#include "ArisBasics.h"
#include "commands.h"

namespace Aris {
  namespace Network {

    // Generates commands for sending to sonar
    class CommandBuilder {
    public:
      static aris::Command SetFrameStreamReceiver(unsigned int port);
      static aris::Command SetFrameStreamReceiver(const char * ipv4Address, unsigned int port);
      static aris::Command SetFrameStreamSettings(bool interpacketDelayEnable, unsigned interpacketDelayMicroseconds);
      static aris::Command RequestAcousticSettings(const Aris::Common::AcousticSettings & settings);
      static aris::Command Ping();
      static aris::Command SetTelephotoLens(bool present);
      static aris::Command SetFocusRange(float range); // range is in meters
      static aris::Command ForceFocus(aris::Command::ForceFocus::Direction direction);
      static aris::Command HomeFocus();
      static aris::Command SetTime(); // local time
      static aris::Command SetSalinity(aris::Command::SetSalinity::Salinity salinity);

      static aris::Command SetRotatorAcceleration(aris::Command::Axis axis, float degreesPerSecondSquare);
      static aris::Command SetRotatorMount(aris::Command::SetRotatorMount::Mount mount);
      static aris::Command SetRotatorPosition(aris::Command::Axis axis, float degrees);
      static aris::Command SetRotatorVelocity(aris::Command::Axis axis, float degreesPerSecond);
      static aris::Command StopRotator(aris::Command::Axis axis);
    };

  }
}
