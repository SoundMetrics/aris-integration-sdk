#pragma once

class AvailableSonars
{
public:
  typedef unsigned SerialNumber;
  typedef boost::asio::ip::address address;
  typedef std::function<void(SerialNumber, address)> AddCallback;
  typedef std::function<void(SerialNumber, address, address)> UpdateCallback;

  std::pair<bool, unsigned> GetSerialNumber(const address & addr) const;

  std::vector<SerialNumber> GetSerialNumbers() const;

  // Get the latest sighting time in microsecond UTC clock.
  std::pair<bool, boost::posix_time::ptime> GetLatestSighting(SerialNumber sn) const;

  void AddOrUpdate(SerialNumber sn, const address & addr,
                   AddCallback onAdd, UpdateCallback onUpdate);

  void Remove(SerialNumber sn);

private:
  struct SonarInfo {
    address                   addr;
    boost::posix_time::ptime  lastSighted;
  };

  std::map<address, SerialNumber> ipToSn; // Look up SN by IP of beacon
  std::map<SerialNumber, SonarInfo> snToIp; // Look up last IP used by sonar
};

