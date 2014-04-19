#pragma once

#include "resource.h"

#define MCOMMS_EXPORTS
#ifdef MCOMMS_EXPORTS
#define MCOMMS_API __declspec(dllexport)
#else
#define MCOMMS_API __declspec(dllimport)
#endif

extern "C" int MCOMMS_API MCSSF_Initialize();
extern "C" int MCOMMS_API MCSSF_AddStream(int nMuxId, int nStreamType, int nBitrate, unsigned short nLanguage, int nExtraDataSize, BYTE* pExtraData);
extern "C" int MCOMMS_API MCSSF_GetHeader(int nMuxId, int nStreamId, int* pDataSize, BYTE** ppData);
extern "C" int MCOMMS_API MCSSF_PushMedia(int nMuxId, int nStreamId, LONGLONG nStartTime, LONGLONG nStopTime, BOOL bIsKeyFrame, int nSampleDataSize, BYTE* pSampleData, int* pOutputDataSize, BYTE** ppOutputData);
extern "C" int MCOMMS_API MCSSF_GetIndex(int nMuxId, int nStreamId, int* pDataSize, BYTE** ppData);
extern "C" int MCOMMS_API MCSSF_Uninitialize(int nMuxId);
