// arislog.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "AvailabilityListener.h"
#include "syslog.h"

//---------------------------------------------------------------------------

constexpr struct {
  static const WORD Info = 0x4F;    // White on brown
  static const WORD Warn = 0x4E;    // Yellow on brown
  static const WORD Error = 0x4C;   // Red on brown
  static const WORD LowKey = 0x47;  // Med. white on brown
} meta;

constexpr struct {
  static const WORD LowKey = 0x07;        // Med. white on black
  static const WORD SubduedInfo = 0x08;   // Dark white on black
  static const WORD SubduedWarn = 0x06;   // Dark yellow on black
  static const WORD SubduedError = 0x04;  // Dark red on black
  static const WORD Info = 0x0F;          // White on black
  static const WORD Warn = 0x0E;          // Yellow on black
  static const WORD Error = 0x0C;         // Red on black
} logentry;

constexpr WORD kDefaultConsoleColor = logentry.Info;

HANDLE hConsole = NULL;

void setcolor(WORD color) {
  ::SetConsoleTextAttribute(hConsole, color);
}

inline void tempcolor(WORD color, std::function<void()> action) {
  setcolor(color);
  action();
  // If we don't get back to the default color it's because the app has crashed.
  setcolor(kDefaultConsoleColor);
}

WORD map_severity_to_color(unsigned severity) {
  constexpr unsigned Emergency = 0;     // Emergency: system is unusable
  constexpr unsigned Alert = 1;         // Alert : action must be taken immediately
  constexpr unsigned Critical = 2;      // Critical : critical conditions
  constexpr unsigned Error = 3;         // Error : error conditions
  constexpr unsigned Warning = 4;       // Warning : warning conditions
  constexpr unsigned Notice = 5;        // Notice : normal but significant condition
  constexpr unsigned Informational = 6; // Informational : informational messages
  constexpr unsigned Debug = 7;         // Debug : debug - level messages 

  if (severity <= Error) {
    return logentry.Error;
  }

  if (severity <= Warning) {
    return logentry.Warn;
  }

  if (severity <= Informational) {
    return logentry.Info;
  }

  return logentry.LowKey;
}

//---------------------------------------------------------------------------
// Boost's io_service drives callback handlers and whatnot. The network calls
// used in this program rely on it. It is passed by reference to functions
// that need it, and is outside the scope of main() only so crtc_handler()
// can stop it when the user wishes to exit.
//
// We have only one thread servicing the io_service object, so every packet
// receipt and so on are, by default, nicely synchronized.

static boost::asio::io_service io;

bool terminated = false;

void write_initial_output();
BOOL WINAPI ctrlc_handler(DWORD /*dwCtrlType*/);
void wire_ctrlc_handler();

//-----------------------------------------------------------------------------

int main()
{
  hConsole = ::GetStdHandle(STD_OUTPUT_HANDLE);
  SetConsoleTitleA("arislog");

  wire_ctrlc_handler();
  write_initial_output();

  AvailabilityListener availability(io,
    // onAdd:
    [](auto sn, auto addr) {
      tempcolor(meta.Info, [sn, &addr]() {
        std::cout << "Found ARIS " << sn << " at " << addr.to_string() << '\n';
      });
    },
    // onUpdate:
    [](auto sn, auto oldAddr, auto newAddr) {
      tempcolor(meta.Info, [sn, &oldAddr, &newAddr]() {
        std::cout << "Sonar " << sn << " moved from " << oldAddr.to_string()
          << " to " << newAddr.to_string() << '\n';
      });
    },
    // onExpired:
    [](auto sn) {
      tempcolor(meta.Info, [sn] {
        std::cout << "No longer hearing from ARIS " << sn << '\n';
      });
    },
    // onError:
    [](auto msg) {
      tempcolor(meta.Error, [&msg]() {
        std::cout << "Error: " << msg << '\n';
      });
    }
    );

  Syslog syslog(
    io,
    // onMeta:
    [](auto msg) {
      tempcolor(meta.Info, [&msg]() {
        std::cout << msg << '\n';
      });
    },
    // onError:
    [](auto msg) {
        tempcolor(meta.Error, [&msg]() {
          std::cout << msg << '\n';
        });
    },
    // onMessage:
    [&availability](auto facility, auto severity, auto addr, auto msg) {
      tempcolor(map_severity_to_color(severity), [&]() {
        bool knownAddress;
        unsigned serialNumber;

        std::cout << '[' << facility << '/' << severity << ' ';
        std::tie(knownAddress, serialNumber) = availability.GetSerialNumber(addr);
        if (knownAddress) {
          std::cout << "ARIS " << serialNumber;
        }
        else {
          std::cout << addr.to_string();
        }

        std::cout << "] " << msg << '\n';
      });
  });

  io.run();

  if (terminated) {
    setcolor(meta.Warn);
    std::cout << "arislog terminated.\n";
  }

  setcolor(logentry.LowKey);
  ::CloseHandle(hConsole);

  return 0;
}

//-----------------------------------------------------------------------------

void write_initial_output() {
  constexpr char * dividerDashes = "-----------------------------------------------------------------------------";
  static const struct {
    uint16_t ta;
    const char * msg;
  } initialOutput[] = {
    { meta.Info, "arislog" },
    { meta.LowKey, dividerDashes },
    { meta.LowKey, "This application displays messages received from syslog relays." },
    { meta.LowKey, "Messages are displayed here only after the first connection from this PC." },
    { meta.LowKey, "Messages displayed may be from ARIS and additional sources." },
    { meta.LowKey, dividerDashes },

    { meta.Info,              "Legend:" },
    { meta.Info,              "  arislog application message" },
    { logentry.Info,          "  Informational" },
    { logentry.Warn,          "  Warning" },
    { logentry.Error,         "  Error" },
    { logentry.LowKey,        "  LowKey" },
    { logentry.SubduedInfo,   "  Subdued Informational" },
    { logentry.SubduedWarn,   "  Subdued Warning" },
    { logentry.SubduedError,  "  Subdued Error" },

    { meta.LowKey,            dividerDashes },
    { meta.LowKey,            "" },
  };

  for (const auto & pr : initialOutput) {
    setcolor(pr.ta);
    std::cout << pr.msg << '\n';
  }

  setcolor(logentry.Info);
}

//-----------------------------------------------------------------------------

BOOL WINAPI ctrlc_handler(DWORD /*dwCtrlType*/) {
  terminated = true;
  io.stop();
  constexpr DWORD kWaitForCleanupMs = 1000;
  Sleep(kWaitForCleanupMs);
  return TRUE;
}

//-----------------------------------------------------------------------------

void wire_ctrlc_handler() {

  // This is Windows-specific. For *nix-related OSes, see
  // http://stackoverflow.com/questions/1641182/how-can-i-catch-a-ctrl-c-event-c

  SetConsoleCtrlHandler(ctrlc_handler, TRUE);
}
