# arislog

This program listens for relayed syslog messages and displays them in a Windows console window. Messages are colorized based on the message severity. See `arislog`'s legend for colors, it is displayed on start-up.

# syslog relay

ARIS initializes syslog relay upon a command connection from a PC. The syslog messages are relayed to the IP address of the client software initiating the connection, the client software being ARIScope or your custom client. `arislog` will show no syslog messages before that time. On disconnection, the syslog messages from ARIS will continue to be relayed to the same IP address, so you will see messages between subsequent connections.

> NOTE: these "between connections" messages will be displayed only with ARIS onboard software installed from ARIScope 2.6.3 or later. This update may not be available at the time `arislog` is released.
