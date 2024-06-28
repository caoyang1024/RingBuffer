using System;
using RingBuffer;

Console.WriteLine(Environment.SystemPageSize);

RingBuffer<string> ringBuffer = new RingBuffer<string>(Environment.SystemPageSize);