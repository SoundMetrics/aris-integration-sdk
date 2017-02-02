// Copyright 2010-2017 Sound Metrics corporation
//

#ifndef ARIS_HEADER_UPDATE_H
#define ARIS_HEADER_UPDATE_H

#include <memory.h>

namespace ArisHeaderUpdate
{

/*

-----------------------------------
How to Send a Correct Header Update
-----------------------------------

There are several types below:

    HeaderUpdates is an enumeration of bit flags that indicate which fields are
    to be updated.

    FrameHeaderUpdatePrefix contains values specific to the command.

    FrameHeaderUpdate contains the new values for updated fields, along
    with the m_nUpdateFlags field which specifies of a combination of values from
    enum HeaderUpdates.

    ArisHeaderUpdateMessage combines the FrameHeaderUpdatePrefix and FrameHeaderUpdate
    structs to form a complete message.

    To update ARIS frame header fields, you must initialize and populate an
    ArisFrameHeaderUpdateMsg struct and send it to port 700 of the ARIS via UDP.
    Unlike the ARIS 2 command stream, this is a UDP message and requires no length
    prefix. The entire ArisFrameHeaderUpdateMsg and nothing more must be sent.

Assuming a local variable of type ArisHeaderUpdateMsg named 'data' the following
must hold true for the ARIS to accept the update message:

    data.header.nCommand must equal ArisHeaderUpdate::C_DATA.

    data.header.nPktNum must be non-zero.

    data.header.nPktType must set the ArisHeaderUpdate::UPDATE_FRAME_HEADER flag.

    data.header.nSize must equal sizeof(ArisHeaderUpdate::FrameHeaderUpdate).

What follows is a table of frame header fields that are updated in response to non-
zero ArisHeaderUpdate flags:

    ArisHeaderUpdate Flag                   Frame Header Fields Updated
    ------------------------------------    ----------------------------------------
    UPDATE_VELOCITY                         Velocity
    UPDATE_DEPTH                            Depth
    UPDATE_ALTITUDE                         Altitude
    UPDATE_PITCH                            Pitch
    UPDATE_PITCHRATE                        PitchRate
    UPDATE_ROLL                             Roll
    UPDATE_ROLLRATE                         RollRate
    UPDATE_HEADING                          Heading
    UPDATE_HEADINGRATE                      HeadingRate
    UPDATE_SONARPAN                         SonarPan
    UPDATE_SONARTILT                        SonarTilt
    UPDATE_SONARROLL                        SonarRoll
    UPDATE_LATITUDE                         Latitude
    UPDATE_LONGITUDE                        Longitude
    UPDATE_SONARPOSITION                    SonarPosition
    UPDATE_TARGETRANGE                      TargetRange
    UPDATE_TARGETBEARING                    TargetBearing
    UPDATE_TARGETPRESENT                    TargetPresent
    UPDATE_USERDATA                         UserValue1, UserValue2, UserValue3,
                                            UserValue4, UserValue5, UserValue6,
                                            UserValue7, UserValue8
    UPDATE_SONARTIME                        TS_Year, TS_Month, TS_Day, TS_Hour,
                                            TS_Minute, TS_Second, TS_HSecond
    UPDATE_DEGC2                            DegC2
    UPDATE_FRAMENUMBER                      FrameIndex
    UPDATE_WATERTEMP                        WaterTemp
    UPDATE_SONARX                           SonarX
    UPDATE_SONARY                           SonarY
    UPDATE_SONARZ                           SonarZ
    UPDATE_VEHICLETIME                      VehicleTime
    UPDATE_GGK                              TimeGGK, DateGGK, QualityGGK,
                                            NumSatsGGK, DOPGGK, EHTGGK
    UPDATE_PANOFFSET                        SonarPanOffset
    UPDATE_TILTOFFSET                       SonarTiltOffset
    UPDATE_ROLLOFFSET                       SonarRollOffset
    UPDATE_RSVD5                            (no effect)

 */

#define ARIS_HEADER_UPDATE_PORT 700

#pragma pack(push)
#pragma pack(1)

    enum HeaderUpdates
    {
       // FrameHeaderUpdate structure flag definitions for m_nUpdateFlags member

        UPDATE_VELOCITY      = 0x00000001,
        UPDATE_DEPTH         = 0x00000002,
        UPDATE_ALTITUDE      = 0x00000004,
        UPDATE_PITCH         = 0x00000008,
        UPDATE_PITCHRATE     = 0x00000010,
        UPDATE_ROLL          = 0x00000020,
        UPDATE_ROLLRATE      = 0x00000040,
        UPDATE_HEADING       = 0x00000080,
        UPDATE_HEADINGRATE   = 0x00000100,
        UPDATE_SONARPAN      = 0x00000200,
        UPDATE_SONARTILT     = 0x00000400,
        UPDATE_SONARROLL     = 0x00000800,
        UPDATE_LATITUDE      = 0x00001000,
        UPDATE_LONGITUDE     = 0x00002000,
        UPDATE_SONARPOSITION = 0x00004000,
        UPDATE_TARGETRANGE   = 0x00008000,
        UPDATE_TARGETBEARING = 0x00010000,
        UPDATE_TARGETPRESENT = 0x00020000,
        UPDATE_USERDATA      = 0x00040000,
        UPDATE_SONARTIME     = 0x00080000,
        UPDATE_DEGC2         = 0x00100000,
        UPDATE_FRAMENUMBER   = 0x00200000,
        UPDATE_WATERTEMP     = 0x00400000,
        UPDATE_SONARX        = 0x00800000,
        UPDATE_SONARY        = 0x01000000,
        UPDATE_SONARZ        = 0x02000000,
        UPDATE_VEHICLETIME   = 0x04000000,
        UPDATE_GGK           = 0x08000000,
        UPDATE_PANOFFSET     = 0x10000000,
        UPDATE_TILTOFFSET    = 0x20000000,
        UPDATE_ROLLOFFSET    = 0x40000000,
        UPDATE_RSVD5         = 0x80000000,
    };

    enum {
        C_DATA              = 0xa502,
        UPDATE_FRAME_HEADER = 0x0040,
    };


    struct FrameHeaderUpdatePrefix
    {
        unsigned short nCommand;
        unsigned short nSize;
        unsigned short nPktType;
        unsigned short nPktNum;
    };

    struct FrameHeaderUpdate
    {
        float m_fVelocity;
        float m_fDepth;
        float m_fAltitude;
        float m_fPitch;
        float m_fPitchRate;
        float m_fRoll;
        float m_fRollRate;
        float m_fHeading;
        float m_fHeadingRate;
        float m_fSonarPan;
        float m_fSonarTilt;
        float m_fSonarRoll;
        double m_dLatitude;
        double m_dLongitude;
        float m_fSonarPosition;
        float m_fTargetRange;
        float m_fTargetBearing;
        unsigned int m_bTargetPresent;
        unsigned int m_nUpdateFlags; // mask for which positions to overwrite in sonar frame header
        float m_fUser1;
        float m_fUser2;
        float m_fUser3;
        float m_fUser4;
        float m_fUser5;
        float m_fUser6;
        float m_fUser7;
        float m_fUser8;
        unsigned int m_nDegC2;
        unsigned int m_nFrameNumber;
        float m_fWaterTemp;
        float m_fSonarX;
        float m_fSonarY;
        float m_fSonarZ;
        float m_fSonarPanOffset;
        float m_fSonarTiltOffset;
        float m_fSonarRollOffset;
        double m_dVehicleTime;
    };

    struct ArisFrameHeaderUpdateMsg
    {
        FrameHeaderUpdatePrefix header;
        FrameHeaderUpdate update;
    };

#pragma pack(pop)

    // ----------------------------------
    // Sample Functions
    // ----------------------------------

    /* These sample functions initialize and configure an update message with
       latitude and longitude. Calling these functions looks like this:

           ArisHeaderUpdate::ArisFrameHeaderUpdateMsg update;
           ArisHeaderUpdate::InitializeArisFrameHeaderUpdateMsg(&update);
           ArisHeaderUpdate::SetArisFrameHeader_LatLong(&update, 0.025, -91.35);
    */

    inline void InitializeArisFrameHeaderUpdateMsg(ArisFrameHeaderUpdateMsg * msg) {
        
        memset(msg, 0, sizeof(ArisFrameHeaderUpdateMsg));

        msg->header.nCommand = ArisHeaderUpdate::C_DATA;
        msg->header.nPktNum = 1;
        msg->header.nPktType = ArisHeaderUpdate::UPDATE_FRAME_HEADER;
        msg->header.nSize = sizeof(ArisHeaderUpdate::FrameHeaderUpdate);
    }

    inline void SetArisFrameHeader_LatLong(
        ArisFrameHeaderUpdateMsg * msg,
        double latitude,
        double longitude) {

        auto update = &msg->update;
        update->m_nUpdateFlags |= (UPDATE_LATITUDE | UPDATE_LONGITUDE);
        update->m_dLatitude = latitude;
        update->m_dLongitude = longitude;
    }

}

#endif   //  #ifndef ARIS_HEADER_UPDATE_H

