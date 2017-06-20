#pragma once

#include <string>
#include <optional>

struct Args {
  int serialNumber;
  std::optional<bool> useMulticast;
};

struct ArgParseResults {
  bool success;
  std::string errorMessage;
  Args args;
};

ArgParseResults parseArgs(int argc, char **argv);