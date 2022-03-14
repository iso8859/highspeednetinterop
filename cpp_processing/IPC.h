#pragma once
struct exchangeStruct
{
	HANDLE sendEvent;
	HANDLE receiveEvent;

	void* sendData; // Caller is responsible to free sendData memory
	int32_t sendDataLength;

	void* responseData; // This DLL is responsible to free responseData memory
	int32_t responseDataLength;
};

class Processing
{
	exchangeStruct* pData;
	HANDLE exitEvent;
	HANDLE pThread;
	DWORD threadId;
	int exitTimeout = 10000;
public:
	Processing(exchangeStruct* pData);
	~Processing();

	virtual DWORD MainThread();

	virtual void DoProcessing();
	virtual void Dispose();
};

extern "C" {
	DLL_API void* Init(exchangeStruct* pData); // Caller is responsible for pData memory
	DLL_API void Dispose(void* pInstance); // After calling Dispose free sendData

	DLL_API void SlowMarshalingProcess(exchangeStruct* pData);
	DLL_API void SlowMarshalingFree(exchangeStruct* pData);
}