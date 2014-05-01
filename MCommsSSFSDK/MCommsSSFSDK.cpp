#include "stdafx.h"
#include "MCommsSSFSDK.h"
#include <dshow.h>
#include <dvdmedia.h>

#include "../3rdParty/SSFSDK/ssfsdkapi.h"
#pragma comment(lib, "../3rdParty/SSFSDK/ssfsdk.lib")
#pragma comment(lib, "xmllite.lib")

#include <map>

using namespace std;

typedef LONGLONG REFERENCE_TIME;

struct StreamContext
{
    WCHAR szStreamName[MAX_PATH];
    int nBitrate;
    SSF_STREAM_TYPE eStreamType;
    BYTE* pbTypeInfo;
    ULONG cbTypeInfo;
    DWORD dwStreamIndex;
    DWORD dwChunkIndex;
    BOOL fChunkInProgress;
    WORD wLanguage;
    REFERENCE_TIME rtChunkStartTime;
    REFERENCE_TIME rtChunkCurrentTime;
    REFERENCE_TIME rtChunkDuration;
};

struct MuxContext
{
    int nMuxId;
    SSFMUXHANDLE hSSFMux;
    map<int, StreamContext*>* pStreams;
    int nVideoStreams;
    int nAudioStreams;
};

CRITICAL_SECTION g_csInstance;
map<int, MuxContext*>* g_pMuxes = NULL;
int g_nMuxCounter = 0;

int MCOMMS_API MCSSF_Initialize()
{
    int nMuxId = -1;
    SSFMUXHANDLE hSSFMux = NULL;
    HRESULT hr = SSFMuxCreate(&hSSFMux);
    if (FAILED(hr))
    {
        goto done;
    }

    UINT64 timeScale = 10000000;
    hr = SSFMuxSetOption(hSSFMux, SSF_MUX_OPTION_TIME_SCALE, &timeScale, sizeof(timeScale));
    if (FAILED(hr))
    {
        goto done;
    }

    //BOOL fVariableBitRate = TRUE;
    //hr = SSFMuxSetOption(hSSFMux, SSF_MUX_OPTION_VARIABLE_RATE, &fVariableBitRate, sizeof(fVariableBitRate));
    //if (FAILED(hr))
    //{
    //    goto done;
    //}

    BOOL fLive = TRUE;
    hr = SSFMuxSetOption(hSSFMux, SSF_MUX_OPTION_LIVE_MODE, &fLive, sizeof(fLive));
    if (FAILED(hr))
    {
        goto done;
    }

    BOOL fEnable = TRUE;
    hr = SSFMuxSetOption(hSSFMux, SSF_MUX_OPTION_ENABLE_STREAM_MANIFEST_BOX_FOR_LIVE_MODE, &fEnable, sizeof(fEnable));
    if (FAILED(hr))
    {
        goto done;
    }

    MuxContext* pMux = new MuxContext();
    ZeroMemory(pMux, sizeof(MuxContext));

    pMux->pStreams = new map<int, StreamContext*>();

    EnterCriticalSection(&g_csInstance);

    if (g_pMuxes == NULL)
    {
        g_pMuxes = new map<int, MuxContext*>();
    }

    nMuxId = pMux->nMuxId = ++g_nMuxCounter;
    pMux->hSSFMux = hSSFMux;
    g_pMuxes->insert(make_pair(nMuxId, pMux));

    LeaveCriticalSection(&g_csInstance);

done:

    if (nMuxId < 0)
    {
        if (NULL != hSSFMux)
        {
            SSFMuxDestroy(hSSFMux);
        }
    }

    return nMuxId;
}

int MCOMMS_API MCSSF_AddStream(int nMuxId, int nStreamType, int nBitrate, unsigned short nLanguage, int nExtraDataSize, BYTE* pExtraData)
{
    int nStreamId = -1;
    MuxContext* pMux = NULL;

    EnterCriticalSection(&g_csInstance);

    map<int, MuxContext*>::iterator i_m = g_pMuxes->find(nMuxId);
    if (i_m != g_pMuxes->end())
    {
        pMux = i_m->second;
    }

    LeaveCriticalSection(&g_csInstance);

    if (pMux == NULL)
    {
        return -1;
    }

    StreamContext* pStream = new StreamContext();
    ZeroMemory(pStream, sizeof(StreamContext));

    pStream->eStreamType = (SSF_STREAM_TYPE)nStreamType;
    pStream->nBitrate = nBitrate;
    pStream->wLanguage = nLanguage;
    if (nExtraDataSize > 0)
    {
        pStream->cbTypeInfo = nExtraDataSize;
        pStream->pbTypeInfo = new BYTE[nExtraDataSize];
        memcpy(pStream->pbTypeInfo, pExtraData, nExtraDataSize);
    }

    //MPEG2VIDEOINFO* pVih2 = (MPEG2VIDEOINFO*)pStream->pbTypeInfo;
    //BYTE* pExtra = (BYTE*)&pVih2->dwSequenceHeader[0];
    //BYTE* pExtra1 = (BYTE*)(pVih2 + 1);
    //int nSize = SIZE_MPEG2VIDEOINFO(pVih2);
    WAVEFORMATEX* pWfx = (WAVEFORMATEX*)pStream->pbTypeInfo;
    BYTE* pExtra = (BYTE*)(pWfx + 1);

    if (pStream->eStreamType == SSF_STREAM_AUDIO)
    {
        swprintf_s(pStream->szStreamName, MAX_PATH, L"Audio%d.isma", pMux->nAudioStreams++);
        pStream->rtChunkDuration = 50000000; // 5 seconds in hns units
    }
    else if (pStream->eStreamType == SSF_STREAM_VIDEO)
    {
        swprintf_s(pStream->szStreamName, MAX_PATH, L"Video%d.ismv", pMux->nVideoStreams++);
        pStream->rtChunkDuration = 20000000; // 2 seconds in hns units
    }

    SSF_STREAM_INFO streamInfo;
    ZeroMemory(&streamInfo, sizeof(streamInfo));
    streamInfo.streamType = pStream->eStreamType;
    streamInfo.dwBitrate = pStream->nBitrate;
    streamInfo.pszSourceFileName = pStream->szStreamName;
    streamInfo.pTypeSpecificInfo = pStream->pbTypeInfo;
    streamInfo.cbTypeSpecificInfo = pStream->cbTypeInfo;
    streamInfo.wLanguage = pStream->wLanguage;

    HRESULT hr = SSFMuxAddStream(pMux->hSSFMux, &streamInfo, &pStream->dwStreamIndex);
    if (SUCCEEDED(hr))
    {
        nStreamId = pStream->dwStreamIndex;
        pMux->pStreams->insert(make_pair(nStreamId, pStream));
    }
    else
    {
        delete pStream;
    }

    return nStreamId;
}

int MCOMMS_API MCSSF_GetHeader(int nMuxId, int nStreamId, int* pDataSize, BYTE** ppData)
{
    MuxContext* pMux = NULL;

    EnterCriticalSection(&g_csInstance);

    map<int, MuxContext*>::iterator i_m = g_pMuxes->find(nMuxId);
    if (i_m != g_pMuxes->end())
    {
        pMux = i_m->second;
    }

    LeaveCriticalSection(&g_csInstance);

    if (pMux == NULL)
    {
        return -1;
    }

    map<int, StreamContext*>::iterator i_s = pMux->pStreams->find(nStreamId);
    if (i_s == pMux->pStreams->end())
    {
        return -1;
    }

    StreamContext* pStream = i_s->second;

    SSF_BUFFER outputBuffer;
    HRESULT hr = SSFMuxGetHeader(pMux->hSSFMux, &pStream->dwStreamIndex, 1, &outputBuffer);
    if (FAILED(hr))
    {
        return -1;
    }

    *pDataSize = outputBuffer.cbBuffer;
    *ppData = outputBuffer.pbBuffer;

    return 1;
}

int MCOMMS_API MCSSF_PushMedia(int nMuxId, int nStreamId, LONGLONG nStartTime, LONGLONG nStopTime, BOOL bIsKeyFrame, int nSampleDataSize, BYTE* pSampleData, int* pOutputDataSize, BYTE** ppOutputData)
{
    MuxContext* pMux = NULL;

    EnterCriticalSection(&g_csInstance);

    map<int, MuxContext*>::iterator i_m = g_pMuxes->find(nMuxId);
    if (i_m != g_pMuxes->end())
    {
        pMux = i_m->second;
    }

    LeaveCriticalSection(&g_csInstance);

    if (pMux == NULL)
    {
        return -1;
    }

    map<int, StreamContext*>::iterator i_s = pMux->pStreams->find(nStreamId);
    if (i_s == pMux->pStreams->end())
    {
        return -1;
    }

    StreamContext* pStream = i_s->second;

    pStream->rtChunkCurrentTime = nStartTime;
    HRESULT hr;
    int nResult = 0;

    if (bIsKeyFrame && (pStream->rtChunkCurrentTime - pStream->rtChunkStartTime >= pStream->rtChunkDuration))
    {
        hr = SSFMuxAdjustDuration(pMux->hSSFMux, pStream->dwStreamIndex, nStartTime);
        if (FAILED(hr))
        {
            return -1;
        }

        SSF_BUFFER outputBuffer;
        hr = SSFMuxProcessOutput(pMux->hSSFMux, pStream->dwStreamIndex, &outputBuffer);
        if (FAILED(hr))
        {
            return -1;
        }

        ++pStream->dwChunkIndex;
        pStream->fChunkInProgress = FALSE;

        *pOutputDataSize = outputBuffer.cbBuffer;
        *ppOutputData = outputBuffer.pbBuffer;
        nResult = 1;
    }

    SSF_SAMPLE inputSample = { 0 };
    inputSample.qwSampleStartTime = (UINT64)nStartTime;
    inputSample.pSampleData = pSampleData;
    inputSample.cbSampleData = nSampleDataSize;
    inputSample.flags = SSF_SAMPLE_FLAG_START_TIME;

    if (bIsKeyFrame)
    {
        inputSample.FrameType = FRAMETYPE_I;
    }

    if (nStopTime > 0)
    {
        inputSample.qwSampleDuration = nStopTime - nStartTime;
        inputSample.flags |= SSF_SAMPLE_FLAG_DURATION;
    }

    if (!pStream->fChunkInProgress)
    {
        pStream->rtChunkStartTime = nStartTime;
        pStream->fChunkInProgress = TRUE;
    }

    hr = SSFMuxProcessInput(pMux->hSSFMux, pStream->dwStreamIndex, &inputSample);
    if (FAILED(hr))
    {
        return -1;
    }

    return nResult;
}

int MCOMMS_API MCSSF_GetIndex(int nMuxId, int nStreamId, int* pDataSize, BYTE** ppData)
{
    MuxContext* pMux = NULL;

    EnterCriticalSection(&g_csInstance);

    map<int, MuxContext*>::iterator i_m = g_pMuxes->find(nMuxId);
    if (i_m != g_pMuxes->end())
    {
        pMux = i_m->second;
    }

    LeaveCriticalSection(&g_csInstance);

    if (pMux == NULL)
    {
        return -1;
    }

    map<int, StreamContext*>::iterator i_s = pMux->pStreams->find(nStreamId);
    if (i_s == pMux->pStreams->end())
    {
        return -1;
    }

    StreamContext* pStream = i_s->second;

    SSF_BUFFER outputBuffer;
    HRESULT hr = SSFMuxGetIndex(pMux->hSSFMux, &pStream->dwStreamIndex, 1, &outputBuffer);
    if (FAILED(hr))
    {
        return -1;
    }

    *pDataSize = outputBuffer.cbBuffer;
    *ppData = outputBuffer.pbBuffer;

    return 1;
}

int MCOMMS_API MCSSF_Uninitialize(int nMuxId)
{
    int nResult = 1;

    EnterCriticalSection(&g_csInstance);

    if (g_pMuxes != NULL)
    {
        map<int, MuxContext*>::iterator i_m = g_pMuxes->find(nMuxId);
        if (i_m != g_pMuxes->end())
        {
            MuxContext* pMux = i_m->second;

            if (pMux->pStreams)
            {
                for (map<int, StreamContext*>::iterator i_s = pMux->pStreams->begin(); i_s != pMux->pStreams->end(); ++i_s)
                {
                    if (i_s->second->pbTypeInfo)
                    {
                        delete[] i_s->second->pbTypeInfo;
                    }
                    delete i_s->second;
                }
            }

            if (NULL != pMux->hSSFMux)
            {
                SSFMuxDestroy(pMux->hSSFMux);
            }

            delete pMux;
            g_pMuxes->erase(i_m);
        }
        else
        {
            nResult = 0;
        }

        if (g_pMuxes->size() == 0)
        {
            delete g_pMuxes;
            g_pMuxes = NULL;
        }
    }

    LeaveCriticalSection(&g_csInstance);

    return nResult;
}
