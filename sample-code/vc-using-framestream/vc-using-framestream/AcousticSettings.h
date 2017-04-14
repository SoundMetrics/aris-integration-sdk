#pragma once

#include "ArisBasics.h"
#include <cstdint>

constexpr uint32_t kFirstSettingsCookie = 1;
constexpr uint32_t kNoSettingsCookie = 0;

// Generates a monotonically increasing sequence for use as cookies
// in the "request settings" command.
class CookieSequence {
public:
  CookieSequence() : nextCookie_(kFirstSettingsCookie) { }

  uint32_t Next() { return nextCookie_++; }

private:
  uint32_t nextCookie_;
};

//-----------------------------------------------------------------------------

// Default acoustic settings for initial integrator bring-up per the SDK
// document, ordered by system type:
//  1800: 0
//  3000: 1
//  1200: 2
constexpr AcousticSettings DefaultAcousticSettingsForSystem[3] = {
  { // 1800
    0,        // cookie
    15.0,     // frameRate
    3,        // pingMode
    SonarFrequency::High, // frequency
    1024,     // samplesPerBeam
    2028,     // sampleStartDelay
    10500,    // cyclePeriod
    8,        // samplePeriod
    11,       // pulseWidth
    true,     // enableTransmit
    true,     // enable150Volts
    18,       // receiverGain
  },

  { // 3000
    0,        // cookie
    15.0,     // frameRate
    9,        // pingMode
    SonarFrequency::High, // frequency
    946,      // samplesPerBeam
    2028,     // sampleStartDelay
    7118,     // cyclePeriod
    5,        // samplePeriod
    10,       // pulseWidth
    true,     // enableTransmit
    true,     // enable150Volts
    12,       // receiverGain
  },

  { // 1200
    0,        // cookie
    10.0,     // frameRate
    1,        // pingMode
    SonarFrequency::High, // frequency
    1082,     // samplesPerBeam
    5408,     // sampleStartDelay
    32818,    // cyclePeriod
    25,       // samplePeriod
    24,       // pulseWidth
    true,     // enableTransmit
    true,     // enable150Volts
    20,       // receiverGain
  },
};
