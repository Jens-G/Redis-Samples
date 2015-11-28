# Scaling Sample

In this sample we 

- use Redis lists as FIFO-queues 
- to feed cluster of worker processes with tasks 
- given by client processes
- all observed by monitor processes. 

Clients, workers and monitor processes may be added or removed at any time to increase or decrease workload or processing throughput.

