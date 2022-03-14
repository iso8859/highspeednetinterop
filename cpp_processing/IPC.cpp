#include "pch.h"
#include "IPC.h"

void* Init(exchangeStruct* pData)
{
	return new Processing(pData);
}

void Dispose(void* pInstance)
{
	delete ((Processing*)pInstance);
}

void SlowMarshalingProcess(exchangeStruct* pData)
{
	SlowMarshalingFree(pData);
	pData->responseData = malloc(pData->sendDataLength);
	pData->responseDataLength = pData->sendDataLength;
	CopyMemory(pData->responseData, pData->sendData, pData->sendDataLength);
}

void SlowMarshalingFree(exchangeStruct* pData)
{
	if (pData->responseData != NULL)
		free(pData->responseData);
	pData->responseData = NULL;
}

static DWORD WINAPI StaticThreadStart(void* Param)
{
	Processing* This = (Processing*)Param;
	return This->MainThread();
}

Processing::Processing(exchangeStruct* pData)
{
	this->pData = pData;
	exitEvent = CreateEvent(NULL, true, false, NULL);
	pThread = CreateThread(NULL, 0, StaticThreadStart, this, 0, &threadId);
}

Processing::~Processing()
{
	SetEvent(exitEvent);
	WaitForSingleObject(pThread, exitTimeout);
	Dispose();
}


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

void Processing::DoProcessing()
{
	// In this example we copy sent data to response
	Dispose();
	pData->responseData = malloc(pData->sendDataLength);
	pData->responseDataLength = pData->sendDataLength;
	CopyMemory(pData->responseData, pData->sendData, pData->sendDataLength);
}

void Processing::Dispose()
{
	if (pData->responseData != NULL)
		free(pData->responseData);
	pData->responseData = NULL;
}