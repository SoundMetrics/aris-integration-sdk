#pragma once

#include <vector>

class Frame
{
public:
  Frame(std::vector<uint8_t> && header, std::vector<uint8_t> && samples);
  ~Frame();

private:
  std::vector<uint8_t> header_, samples_;
};

