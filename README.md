low level functional-style networking experiment for large game worlds with Unity ECS+Jobs+Burst

traditionally most networking methods are either:
- quake style (send all info every frame, + delta compression), usually udp unreliable packets
- explicit network packets, inlined with gameplay logic, usually reliable packets

this is an experiment to extend the quake model to support very large worlds (many thousands of players) over UDP and provide an ergonomic API.


procedure:
- densely pack game world & all actors
- diff(old_world, new_world)
- per-client diff(old_client_view, new_world)
- encode & send diff