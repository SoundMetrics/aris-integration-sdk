#include "args.h"
#include <algorithm>

ArgParseResults parseArgs(int argc, char **argv) {

  ArgParseResults results;
  bool foundSN = false;

  results.success = true;
  results.errorMessage = "none";

  auto begin = std::next(argv); // skip arg 0
  auto end = argv + argc;

  std::for_each(begin, end, [&results, &foundSN](auto arg) {
    int sn;
    auto sArg = std::string(arg);

    if (sArg == "-b") {
      results.args.useBroadcast = true;
    }
    else if ((sn = atoi(arg)) != 0) {
      if (foundSN) {
        results.errorMessage = "multiple serial numbers given";
        results.success = false;
      }

      results.args.serialNumber = sn;
      foundSN = true;
    }
    else {
      results.success = false;
      results.errorMessage = "unexpected argument: " + sArg;
    }
  });

  results.success = results.success && foundSN;
  if (!foundSN) {
    results.errorMessage = "no serial number given";
  }

  return results;
}
