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