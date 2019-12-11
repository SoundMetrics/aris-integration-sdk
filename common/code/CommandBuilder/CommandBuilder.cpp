// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.

#include "CommandBuilder.h"
#include <sstream>
#include <time.h>

namespace Aris {
  namespace Network {

    using namespace aris;

    aris::Command CommandBuilder::SetFrameStreamReceiver(unsigned int port)
    {
      Command command;
      command.set_type(Command::SET_FRAMESTREAM_RECEIVER);

      auto receiver = command.mutable_framestreamreceiver();
      receiver->set_port(port);
      //receiver->set_ip(receiverIpAddr);

      return command;
    }

    aris::Command CommandBuilder::SetFrameStreamReceiver(const char * ipv4Address, unsigned int port)
    {
      Command command;
      command.set_type(Command::SET_FRAMESTREAM_RECEIVER);

      auto receiver = command.mutable_framestreamreceiver();
      receiver->set_port(port);
      receiver->set_ip(ipv4Address);

      return command;
    }

    aris::Command CommandBuilder::SetFrameStreamSettings(bool interpacketDelayEnable, unsigned interpacketDelayMicroseconds)
    {
      Command command;
      command.set_type(Command::SET_FRAMESTREAM_SETTINGS);

      auto settings = command.mutable_framestreamsettings();
      auto pd = settings->mutable_interpacketdelay();
      pd->set_enable(interpacketDelayEnable);
      pd->set_delayperiod(interpacketDelayMicroseconds);

      return command;
    }

    aris::Command CommandBuilder::RequestAcousticSettings(const Aris::Common::AcousticSettings & settings)
    {
      Command command;
      command.set_type(Command::SET_ACOUSTICS);

      auto settings_ = command.mutable_settings();
      settings_->set_cookie(settings.cookie);
      settings_->set_framerate(settings.frameRate);
      settings_->set_samplesperbeam(settings.samplesPerBeam);
      settings_->set_samplestartdelay(settings.sampleStartDelay);
      settings_->set_cycleperiod(settings.cyclePeriod);
      settings_->set_sampleperiod(settings.samplePeriod);
      settings_->set_pulsewidth(settings.pulseWidth);
      settings_->set_pingmode(settings.pingMode);
      settings_->set_enabletransmit(settings.enableTransmit);
      settings_->set_frequency((Command::SetAcousticSettings::Frequency)settings.frequency);
      settings_->set_enable150volts(settings.enable150Volts);
      settings_->set_receivergain(settings.receiverGain);

      return command;
    }

    aris::Command CommandBuilder::SetTelephotoLens(bool present)
    {
      Command command;
      command.set_type(Command::SET_TELEPHOTO);

      auto telephoto = command.mutable_telephoto();
      telephoto->set_telephoto(present);

      return command;
    }

    aris::Command CommandBuilder::SetFocusRange(float range)
    {
      Command command;
      command.set_type(Command::SET_FOCUS);

      auto position = command.mutable_focusposition();
      position->set_focusrange(range);

      return command;
    }

    aris::Command CommandBuilder::ForceFocus(aris::Command::ForceFocus::Direction direction)
    {
      Command command;
      command.set_type(Command::FORCE_FOCUS);

      auto force = command.mutable_focusforce();
      force->set_direction(direction);

      return command;
    }

    aris::Command CommandBuilder::HomeFocus()
    {
      Command command;
      command.set_type(Command::HOME_FOCUS);

      command.mutable_focushome();

      return command;
    }

    aris::Command CommandBuilder::Ping()
    {
      Command command;
      command.set_type(Command::PING);

      command.mutable_ping();

      return command;
    }

    static const char* const shortmonth_enUS[] = {
        "Jan",
        "Feb",
        "Mar",
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
    };

    /*
        ARIS expects the datetime message it receives to be formatted like this:
        "2019-Apr-01 13:24:35". ARIS expects the month to be the short month as
        defined within the en_US locale. Unfortunately, use of strftime() may
        break some integration partners, especially those who may install their
        client software on a non-en_US PC.

        Using setlocale() is not thread-safe, so we avoid switching locale to en_US
        and back. This function illustrates constructing the datetime string with
        en_US month names.
    */
    static void format_invariant_datetime(char* const buf, const size_t bufSize) {

      struct tm now;
      const time_t rawtime = time(NULL);

#ifdef _MSC_VER
      localtime_s(&now, &rawtime);
#else
      localtime_r(&rawtime, &now);
#endif

      sprintf_s(
        buf,
        bufSize,
        "%d-%s-%d %02d:%02d:%02d",
        now.tm_year + 1900,
        shortmonth_enUS[now.tm_mon],
        now.tm_mday,
        now.tm_hour,
        now.tm_min,
        now.tm_sec
      );
    }

    aris::Command CommandBuilder::SetTime()
    {
      Command command;
      command.set_type(Command::SET_DATETIME);

      auto pDateTime = command.mutable_datetime();
      char now_str[64];

      format_invariant_datetime(now_str, sizeof(now_str) / sizeof(now_str[0]));
      pDateTime->set_datetime(now_str);

      return command;
    }

    aris::Command CommandBuilder::SetSalinity(aris::Command::SetSalinity::Salinity salinity)
    {
      Command command;
      command.set_type(Command::SET_SALINITY);

      command.mutable_salinity()->set_salinity(salinity);

      return command;
    }

    aris::Command CommandBuilder::SetRotatorAcceleration(aris::Command::Axis axis, float degreesPerSecondSquare)
    {
      Command command;
      command.set_type(Command::SET_ROTATOR_ACCELERATION);

      auto acc = command.mutable_rotatoracceleration();
      acc->set_axis(axis);
      acc->set_acceleration(degreesPerSecondSquare);

      return command;
    }

    aris::Command CommandBuilder::SetRotatorMount(aris::Command::SetRotatorMount::Mount mount)
    {
      Command command;
      command.set_type(Command::SET_ROTATOR_MOUNT);

      command.mutable_mount()->set_mount(mount);

      return command;
    }

    aris::Command CommandBuilder::SetRotatorPosition(aris::Command::Axis axis, float degrees)
    {
      Command command;
      command.set_type(Command::SET_ROTATOR_POSITION);

      auto pos = command.mutable_rotatorposition();
      pos->set_axis(axis);
      pos->set_position(degrees);

      return command;
    }

    aris::Command CommandBuilder::SetRotatorVelocity(aris::Command::Axis axis, float degreesPerSecond)
    {
      Command command;
      command.set_type(Command::SET_ROTATOR_VELOCITY);

      auto v = command.mutable_rotatorvelocity();
      v->set_axis(axis);
      v->set_velocity(degreesPerSecond);

      return command;
    }

    aris::Command CommandBuilder::StopRotator(aris::Command::Axis axis)
    {
      Command command;
      command.set_type(Command::STOP_ROTATOR);

      command.mutable_rotatorstop()->set_axis(axis);

      return command;
    }

  }
}
