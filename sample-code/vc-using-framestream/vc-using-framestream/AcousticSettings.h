#pragma once

#include <cstdint>

enum class SonarFrequency : uint32_t { Low = 0, High = 1 };

struct AcousticSettings {
  uint32_t        cookie;
  float           frameRate;
  uint32_t        pingMode;
  SonarFrequency  frequency;
  uint32_t        samplesPerBeam;
  uint32_t        sampleStartDelay;
  uint32_t        cyclePeriod;
  uint32_t        samplePeriod;
  uint32_t        pulseWidth;
  bool            enableTransmit;
  bool            enable150Volts;
  float           receiverGain;
};

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

class CookieSequence {
public:
  CookieSequence() : nextCookie_(1) { }

  uint32_t Next() { return nextCookie_++; }

private:
  uint32_t nextCookie_;
};
