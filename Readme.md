# `MonitorDictionary<T>` Class

Row level kind of locking inside a .Net process.

## Introduction

Consider the following situation:

Suppose you have a keyed collection of mutatables that don't
share a common state, if their key is different. Assume furthermore,
that it is not possible to lock these objects individually, but only
their keys. This can happen, for example, if 

* the mutatables themselves reside on a remote storage media and are 
  to big to load into memory
* when the mutatables have in fact value semantics, e.g. an array of
  counters
* such a mutatable consists of multiple CLR objects that cannot be
  aggregated or locked individually (deadlock danger)
* multiple, separatly syncronized threads change a single mutatable
  that don't have access to an individual lock

In a multithreaded environment, you can and might want to
change these mutatables with different keys concurrently, but you have to
serialize access to the same objects (with the same key, of course) with
a mutex.

The most familiar situation is encountered in an RDBMS, when the database
issues exclusive *row level locks* to let the client code update rows
with different primary keys concurrently. But it suspends threads that
intend to change already locked rows.

This class is an implementation of this behaviour in memory of a .Net
core process, where `T` is the type of the keys. It builds a
`Dictionary<T>` of the keys and uses `Monitor` to lock individual keys
plus a `CountdownEvent` for reference counting. I.e. to know, when
to remove a key from the dictionary.

## Artificial C# Code Sample

Without concurrency:

````
var transactions = new MonitorDictionary<int>();
var ints = new int[100]();
var index = 5;

using (transactions.Guard(index))
    ints[index]++; // will be CPU or IO intense operation
````

With concurrency:

````
var transactions = new MonitorDictionary<int>();
var ints = new int[100]();
var rnd = new Random();

for(;;)
{
    var index = rnd.Next(ints.Length);
    ThreadPool.QueueUserWorkItem((o) =>
    {
        using (transactions.Guard(index))
        {
            ints[index]++; // will be CPU or IO intense operation
        }
    });
}
````

## Public Methods

`IDisposable Guard(T key)` - Creates or finds `key` in the underlying
dictionary and enters a Monitor. Blocks only, when the `key` was found.
The Monitor is exited when the return value is disposed of. The next
blocked thread, waiting on the `key` is released. If there was no other
waiting thread, the key is removed from the underlying dictionary.

`void AssertIsClearAfterUse()` - *Only for testing.* Prints some
statistics to the Console. Throws, if the collection was not used at all,
if the dictionary is not empty or, if the concurrency level of the
test was to low.

## Sample Timing Explained

This program was run on a current Core i5 processor, i.e. 2 cores 4 threads. Please see the file `SampleOutput.txt` for reference.

### Sample Timing Explained - Part 1

It starts 10 threads to repeatedly read and compare counts form 
a 10 elements long int array plus sleeping a random time.

Furthermore, it starts 10 threads to repeatedly lock and 
increment counts on this array with nearly constant simulated 
operation time. The sleep time of these threads increases by
the index into the array.

These 20 threads are allowed to run for about 30 seconds. 
All threads do the bookkeeping in a separate array, only 
accessed by Interlocked, to 'know the truth'.

````
Stopping in 30s ....

----------- interlocked -----------
  0 -> 111.977
  1 -> 1.910
  2 -> 1.351
  3 -> 1.017
  4 ->   818
  5 ->   699
  6 ->   622
  7 ->   550
  8 ->   511
  9 ->   469
Sum: 119.924

----------- monitored -----------
  0 -> 111.977
  1 -> 1.910
  ...
````

In the two tables we see, how often an index was incremented
in this timespan and that the figures for the interlocked
array and the array locked by `MontitorDictionary<int>` are
identical.

### Sample Timing Explained - Part 2
It does busy waiting to simulate CPU intense work, however, it reads 
a randomly selected (2d6) int from an array with 11 items, waits a 
little bit and writes the incremented value back to the array. Repeat.

````
sequential: 00:00:12.0688195
unlocked: 00:00:03.2108830
monitored: 00:00:04.1203412
0 current keys, 5 max keys, 5 max concurrent use count
globallock: 00:00:12.3116187

----------- sequential -----------
  0 ->   294
  1 ->   572
  2 ->   849
  3 -> 1.102
  ...
````

* sequential - means what it says. Every other timing is measured
  by queuing one increment operation each to the ThreadPool.
* unlocked - no locking at all. Of course, lots of overwritten values,
  i.e.  standard concurrency violation errors, but we see that 4
  threads reduce the time to approximatly one 4th
  of the sequential timing. Should be near the maximum throughput
  possible.
* monitored - using the locking mechanism presented here. The overhead
  of locking slows the figures down to about one 3rd.
* globallock - locking the array as a whole for every access. This
  degenerates the access pattern to the serial one effectively.


## Usage and Incubation Status

Currently only by copying the source in https://github.com/Lercher/MonitorDictionary/blob/master/src/MonitorDictionary.cs
to your project. Please note that this code is fresh, so it 
contains bugs. Handle with care, use at your own risk 
and feel free to fork and improve the code.

## Building the Console Application

Git clone first, then cd to the directory and:

````
dotnet restore
dotnet build
dotnet run
````


## License

https://en.wikipedia.org/wiki/MIT_License
