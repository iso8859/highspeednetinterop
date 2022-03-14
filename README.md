# highspeednetinterop

High speed interop with .NET (all versions)

The objective was to measure the execution time cost of interop with C++ code.

TL;DR; Good news, no effect, you can go, with interop.

This project is also a good example on how to use C++ interop from C# if you are new on this subject. You must have a good knowledge of the mechanisms of the C++ world, memory allocations, pointers, memory management.

I had in mind interop have a big cost on execution time. It was a mistake or bad test. 
This is why I created IPC C++ class that start a thread and do a infinite loop waiting for
synchonisation object to read/write common C++/C# data.

Have a look at IPC.CPP

````
DWORD Processing::MainThread()
{
	int counter = 0;
	HANDLE* h = new HANDLE[2]{ pData->sendEvent, exitEvent };
	while (WaitForSingleObject(exitEvent, 0) != WAIT_OBJECT_0)
	{
		if (WaitForMultipleObjects(2, h, false, INFINITE) == WAIT_OBJECT_0)
		{
			DoProcessing();			
			counter++;
			ResetEvent(pData->sendEvent); // reset here to be ready for next loop
			SetEvent(pData->receiveEvent); // Tell the caller processing is done
		}
	}
	return counter;
}
````

In ImageController.cs you'll see all API entry point.

The 3 methods have almost the same execution time.

Tested with Apache AB.

````
ab.exe -n 50000 -c 4 http://localhost:5000/Image/managed/hello
````

I had to start 16 ab.exe in parallel to go to Kestrel limit.

On my AMD 7 2700X 8 cores (16 threads) I can achieve
- 10868 request/s for Int1 interop implementation
- 11180 request/s for Int2 interop implementation
- 11172 request/s for 100% managed code

What conclusion ?

Interop have almost no cost because the Marshal calls have almost no cost.

Synchronisation object ManualResetEvent is also very cheap in execution time.

Of course we spent more time on Int1 because the implementation is more complex and uses many system calls.

No unsafe code here, Marshal class is very well designed.
