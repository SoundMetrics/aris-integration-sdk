#include "stdafx.h"
#include "AvailableSonars.h"


std::pair<bool, unsigned>
AvailableSonars::GetSerialNumber(const boost::asio::ip::address & addr) const {
  
  const auto found = ipToSn.find(addr);
  if (found != ipToSn.end()) {
    return { true, found->second };
  }

  return { false, 0 };
}

std::vector<AvailableSonars::SerialNumber>
AvailableSonars::GetSerialNumbers() const {
  std::vector<SerialNumber> sns;

  for (const auto & i : snToIp) {
    sns.push_back(i.first);
  }

  return sns;
}

std::pair<bool, boost::posix_time::ptime> 
AvailableSonars::GetLatestSighting(SerialNumber sn) const {
  const auto found = snToIp.find(sn);
  if (found != snToIp.end()) {
    return { true, found->second.lastSighted };
  }

  return { false, boost::posix_time::ptime() };
}

void AvailableSonars::AddOrUpdate(
  SerialNumber sn, const address & addr,
  AddCallback onAdd, UpdateCallback onUpdate)
{
  // Very lazy.
  const auto foundSn = snToIp.find(sn);
  if (foundSn != snToIp.end()) {
    const auto lastIp = foundSn->second.addr;
    ipToSn.erase(lastIp);

    if (lastIp != addr) {
      onUpdate(sn, lastIp, addr);
    }
  }
  else {
    onAdd(sn, addr);
  }

  snToIp.erase(sn);
  ipToSn.erase(addr);

  snToIp[sn] = { addr, boost::posix_time::microsec_clock::universal_time() };
  ipToSn[addr] = sn;
}

void AvailableSonars:: Remove(SerialNumber sn) {
  const auto foundSn = snToIp.find(sn);
  if (foundSn != snToIp.end()) {
    const auto addr = foundSn->second.addr;
    snToIp.erase(sn);
    ipToSn.erase(addr);
  }
}
