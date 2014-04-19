/************************************************************************
*                                                                       *
*   Copyright (c) Microsoft Corp. All rights reserved.                  *
*                                                                       *
************************************************************************/

#ifndef SSFSDKAPI_H
#define SSFSDKAPI_H

#if defined(__cplusplus)
extern "C"
{
#endif

/*******************************************************************************
*
* SSF_STREAM_TYPE - Stream typed. Used with SSF_STREAM_INFO.
*
*******************************************************************************/

enum SSF_STREAM_TYPE
{
    SSF_STREAM_AUDIO = 1,
    SSF_STREAM_VIDEO,
    SSF_STREAM_TTML1,
    SSF_STREAM_XML,
    SSF_STREAM_TEXT,
    SSF_STREAM_NONE,
};


/*******************************************************************************
*
* SSF_FRAME_TYPE - Frame type enumeration. Used with SSF_SAMPLE.
*
*******************************************************************************/

enum SSF_FRAME_TYPE
{
    FRAMETYPE_I = 0x00000001,
    FRAMETYPE_P = 0x00000002,
    FRAMETYPE_B = 0x00000004,
    FRAMETYPE_BI = 0x00000008,
    FRAMETYPE_S = 0x00000010,
    FRAMETYPE_UNKNOWN = 0x00
};


/*******************************************************************************
*
* SSF_SAMPLE_FLAGS - Bitmask of flags that indicates which optional fields of
* the SSF_SAMPLE structure are set.
*
*******************************************************************************/

typedef DWORD SSF_SAMPLE_FLAGS;

enum
{
    SSF_SAMPLE_FLAG_START_TIME              = 0x01,
    SSF_SAMPLE_FLAG_DURATION                = 0x02,
    SSF_SAMPLE_FLAG_FRAME_TYPE              = 0x04,
};


/*******************************************************************************
*
* SSF_RANGE - Specfies a range
*
*******************************************************************************/

typedef struct
{
    DWORD dwOffset;
    DWORD dwLength;
} SSF_RANGE;


/*******************************************************************************
*
* SSF_SAMPLE - Specfies the input parameters for SSFMuxProcessInput
*
*******************************************************************************/

typedef struct
{
    //
    // Pointer to buffer containing the sample data
    //
    __bcount(cbSampleData) LPVOID pSampleData;

    //
    // Size of the data in the sample data buffer
    //
    DWORD cbSampleData;

    //
    // Flags that indicates which of the following fields are set
    //
    SSF_SAMPLE_FLAGS flags;

    UINT64 qwSampleStartTime;

    UINT64 qwSampleDuration;

    SSF_FRAME_TYPE FrameType;

    //
    // Pointer to optional buffer containing the user data
    //
    __bcount_opt(cbUserData) LPVOID pUserData;

    //
    // Size of the data in the user data buffer
    //
    DWORD cbUserData;

    //
    // Pointer to an optional array that specifies what parts of the
    // sample to protect when DRM is enabled
    //
    __in_ecount_opt(cProtectedRanges) SSF_RANGE* prgProtectedRanges;

    //
    // Number of elements in the prgProtectedRanges array
    //
    DWORD cProtectedRanges;
} SSF_SAMPLE;


/*******************************************************************************
*
* SSF_BUFFER - Receives the output buffer of some of the SSF API
*
*******************************************************************************/

typedef struct
{
    //
    // Pointer to the buffer containing the data
    //
    __bcount(cbBuffer) BYTE* pbBuffer;

    //
    // Size of the data in the buffer
    //
    DWORD cbBuffer;

    //
    // Start time of the fragment (valid for SSFMuxProcessOutput)
    //
    UINT64 qwTime;

    //
    // Duration of the fragment (valid for SSFMuxProcessOutput)
    //
    UINT64 qwDuration;

} SSF_BUFFER;


/*******************************************************************************
*
* SSF_STREAM_INFO - Describes the stream properties for SSFMuxAddStream
*
*******************************************************************************/

typedef struct
{
    //
    // The type of the stream (audio, video, etc)
    //
    SSF_STREAM_TYPE streamType;

    //
    // Average QUALITY_LEVEL for this stream, in bits per second
    //
    INT32 dwBitrate;

    //
    // Bitrate index for this stream (used in client manifest)
    //
    INT32 dwBitrateID;

    //
    // Optional hardware profile
    //
    DWORD dwHardwareProfile;

    //
    // Pointer to buffer containing the media type specific information
    // according to the stream type:
    //      SSF_STREAM_AUDIO - WAVEFORMATEX
    //      SSF_STREAM_VIDEO - VIDEOINFOHEADER2 or MPEG2VIDEOINFO
    //      SSF_STREAM_TTML1  - not used, it should be empty
    //
    __bcount_opt(cbTypeSpecificInfo) LPCVOID pTypeSpecificInfo;

    //
    // Size of the data in the type specific information buffer
    //
    DWORD cbTypeSpecificInfo;

    //
    // The language of the stream in ISO 639-2/T three character code
    //
    WORD wLanguage;

    //
    // Mandatory source file name to set as the 'src' attribute in the
    // server manifest. Example: "Video.ismv"
    //
    LPCWSTR pszSourceFileName;

    //
    // Optional track name to set as the 'Name' attribute of the
    // StreamIndex element in the client manifest and as the
    // 'trackName' parameter in the server manifest. If absent, the
    // SDK will generate a default name.
    //
    LPCWSTR pszTrackName;
}
SSF_STREAM_INFO;


/*******************************************************************************
*
* SSF_XML_STREAM_SPECIFIC_INFO - Describes the type specific info
*                                (see SSF_STREAM_INFO) for streams of
*                                type SSF_STREAM_TEXT
*
*******************************************************************************/

typedef struct
{
    //
    // Optinal string that specifies the MIME type of the content.
    // Its absence indicates the content is not encoded.
    //
    LPCWSTR pszContentEncoding;

    //
    // String that specifies the namespace of the schema for the
    // XML metadata.
    //
    LPCWSTR pszNamespace;

    //
    // Optional string that provides a URL to find the schema
    // corresponding to the namespace.
    //
    LPCWSTR pszSchemaLocation;

    //
    // Indicates if the bitrate fields are set
    //
    BOOL fBitrateInformationPresent;

    //
    // Size in bytes of the decoding buffer for the elementary stream.
    // This field is ignored it fBitrateInformationPresent is FALSE
    //
    DWORD dwBufferSizeDB;

    //
    // Average rate in bits/second for the entire presentation
    //
    DWORD dwAvgBitrate;

    //
    // Max rate in bits/second for any interval of one second
    //
    DWORD dwMaxBitrate;
}
SSF_XML_STREAM_SPECIFIC_INFO;


/*******************************************************************************
*
* SSF_TEXT_STREAM_SPECIFIC_INFO - Describes the type specific info
*                                 (see SSF_STREAM_INFO) for streams of
*                                 type SSF_STREAM_XML
*
*******************************************************************************/

typedef struct
{
    //
    // Optinal string that specifies the MIME type of the content.
    // Its absence indicates the content is not encoded.
    //
    LPCWSTR pszContentEncoding;

    //
    // String that specifies the namespace of the schema for the
    // XML metadata.
    //
    LPCWSTR pszMimeFormat;

    //
    // Indicates if the bitrate fields are set
    //
    BOOL fBitrateInformationPresent;

    //
    // Size in bytes of the decoding buffer for the elementary stream.
    // This field is ignored it fBitrateInformationPresent is FALSE
    //
    DWORD dwBufferSizeDB;

    //
    // Average rate in bits/second for the entire presentation
    //
    DWORD dwAvgBitrate;

    //
    // Max rate in bits/second for any interval of one second
    //
    DWORD dwMaxBitrate;
}
SSF_TEXT_STREAM_SPECIFIC_INFO;


/*******************************************************************************
*
* SSFMUXHANDLE - Handle for the mux object
*
*******************************************************************************/

typedef void * SSFMUXHANDLE;


/*******************************************************************************
*
* SSFMuxCreate - Creates a muxer object
*
*******************************************************************************/

HRESULT __stdcall SSFMuxCreate( __out SSFMUXHANDLE *phSSFMux );


/*******************************************************************************
*
* SSFMuxDestroy - Destroys a muxer and frees its resources
*
*******************************************************************************/

HRESULT __stdcall SSFMuxDestroy( __in SSFMUXHANDLE hSSFMux );



/*******************************************************************************
*
* SSFMuxAddStream - Adds a stream to the muxer
*
*   The type specific info can be:
*       - WAVEFORMATEX for audio
*       - VIDEOINFOHEADER2 for video
*
*******************************************************************************/

HRESULT __stdcall SSFMuxAddStream(
    __in SSFMUXHANDLE hSSFMux,
    __in const SSF_STREAM_INFO *pStreamInfo,
    __out DWORD* pdwStreamIndex );

/*******************************************************************************
*
* SSFMuxAddSparseStream - Adds a sparse stream to the muxer
*
*******************************************************************************/

HRESULT __stdcall SSFMuxAddSparseStream(
    __in SSFMUXHANDLE hSSFMux,
    __in const SSF_STREAM_INFO *pStreamInfo,
    __in DWORD dwParentStreamIndex,
    __in BOOL fOutputToManifest,
    __out DWORD* pdwStreamIndex );


/*******************************************************************************
*
* SSFMuxProcessInput - Feeds the muxer with a sample
*
*******************************************************************************/

HRESULT __stdcall SSFMuxProcessInput(
    __in SSFMUXHANDLE hSSFMux,
    __in DWORD dwStreamIndex,
    __in SSF_SAMPLE *pInputBuffer
    );


/*******************************************************************************
*
* SSFMuxProcessOutput - Retrieves a fragment from the muxer
*
*******************************************************************************/

HRESULT __stdcall SSFMuxProcessOutput(
    __in SSFMUXHANDLE hSSFMux,
    __in DWORD dwStreamIndex,
    __out SSF_BUFFER* pOutputBuffer
    );


/*******************************************************************************
*
*   SSFMuxAdjustDuration - Adjusts the duration for the current segment of the
*   stream specified by the stream index
*
*   qwTime is the time at which the fragment should end
*
*   This API should be used only in the case the application is unable
*   to supply reliable duration for the audio/video samples
*
*******************************************************************************/

HRESULT __stdcall SSFMuxAdjustDuration(
    __in SSFMUXHANDLE hSSFMux,
    __in DWORD dwStreamIndex,
    __in UINT64 qwTime
    );


/*******************************************************************************
*
* SSFMuxGetClientManifest - Retrieves the client manifest
*
*******************************************************************************/

HRESULT __stdcall SSFMuxGetClientManifest(
    __in SSFMUXHANDLE hSSFMux,
    __out SSF_BUFFER* pBuffer );


/*******************************************************************************
*
* SSFMuxGetServerManifest - Retrieves the server manifest
*
*******************************************************************************/

HRESULT __stdcall SSFMuxGetServerManifest(
    __in SSFMUXHANDLE hSSFMux,
    __in LPCWSTR pszClientManifestRelativePath,
    __out SSF_BUFFER* pBuffer );


/*******************************************************************************
*
* SSFMuxGetHeader - Retrieves the header
*
*******************************************************************************/

HRESULT __stdcall SSFMuxGetHeader(
    __in SSFMUXHANDLE hSSFMux,
    __in_ecount(dwStreamCount) const DWORD *pdwStreamIndices,
    __in DWORD dwStreamCount,
    __out SSF_BUFFER *pBuffer );


/*******************************************************************************
*
* SSFMuxGetIndex - Retrieves the index
*
*******************************************************************************/

HRESULT __stdcall SSFMuxGetIndex(
    __in SSFMUXHANDLE hSSFMux,
    __in_ecount(dwStreamCount) const DWORD *pdwStreamIndices,
    __in DWORD dwStreamCount,
    __out SSF_BUFFER* pBuffer );


/*******************************************************************************
*
*   Options supported by SSFMuxSetOption and SSFMuxQueryOption
*
*******************************************************************************/

enum SSF_MUX_OPTION
{
    //
    // Type: unsigned int32
    // If value == 0 (default), disables Live Mode.
    // If value != 0, enables Live Mode.
    //
    SSF_MUX_OPTION_LIVE_MODE,

    //
    // Type: unsigned int32
    // If value == 0, disables the generation of the StreamManifestBox
    // when in Live mode.
    // If value != 0 (default), enables the generation of StreamManifestBox
    // for Live mode.
    //
    SSF_MUX_OPTION_ENABLE_STREAM_MANIFEST_BOX_FOR_LIVE_MODE,

    //
    // Type: unsigned int32
    // If value == 0 (default), the client manifest returned by
    // SSFMuxGetClientManifest will include fragment elements
    // (i.e., the <f> element) only if applicable, otherwise the
    // elements will not be included.
    // If value != 0, SSFMuxGetClientManifest will always include
    // the fragment elements in the manifest.
    //
    SSF_MUX_OPTION_FORCE_FRAGMENT_ELEMENT_IN_CLIENT_MANIFEST,

    //
    // Type: int64
    // Time scale of the time stamps.
    //
    SSF_MUX_OPTION_TIME_SCALE,

    //
    // Type: unsigned int32
    // If value == 0 (default), assumes fixed rate.
    // If value != 0, forces the durations of the frames to be written to the
    // 'trun' box.
    //
    SSF_MUX_OPTION_VARIABLE_RATE,

    //
    // Type: unsigned int32
    // If value == 0 (default), disables PlayReady DRM.
    // If value != 0, enables PlayReady DRM. In this case, the following
    // options must be set too:
    // - SSF_MUX_OPTION_PLAYREADY_KEY_ID
    // - SSF_MUX_OPTION_PLAYREADY_KEY_SEED or SSF_MUX_OPTION_PLAYREADY_CONTENT_KEY
    //
    SSF_MUX_OPTION_ENABLE_PLAYREADY_DRM,

    //
    // Type: GUID
    // The Key ID to use with PlayReady DRM.
    //
    SSF_MUX_OPTION_PLAYREADY_KEY_ID,

    //
    // Type: unsigned char[40]
    // The Key Seed to use with PlayReady DRM.
    //
    SSF_MUX_OPTION_PLAYREADY_KEY_SEED,

    //
    // Type: GUID
    // The Content Key to use with PlayReady DRM.
    //
    SSF_MUX_OPTION_PLAYREADY_CONTENT_KEY,

    //
    // Type: unsigned int []
    // String of Unicode chars representing the License Acquisition URL
    // for PlayReady DRM.
    //
    SSF_MUX_OPTION_PLAYREADY_LICENSE_ACQUISITION_URL,

    //
    // Type: unsigned int []
    // String of Unicode chars representing the UI URL for PlayReady DRM.
    //
    SSF_MUX_OPTION_PLAYREADY_UI_URL,

    //
    // Type: UINT64
    // Initialization Vector for encryption. If one is not given, the muxer
    // will use a random value in place of this.
    //
    SSF_MUX_OPTION_PLAYREADY_INITIALIZATION_VECTOR,
};


/*******************************************************************************
*
*   SSFMuxSetOption
*
*******************************************************************************/

HRESULT __stdcall SSFMuxSetOption(
    __in SSFMUXHANDLE hSSFMux,
    __in SSF_MUX_OPTION option,
    __in_bcount_opt(cbBuffer) LPCVOID pvBuffer,
    __in ULONG cbBuffer );


/*******************************************************************************
*
*   SSFMuxQueryOption
*
*******************************************************************************/

HRESULT __stdcall SSFMuxQueryOption(
    __in SSFMUXHANDLE hSSFMux,
    __in SSF_MUX_OPTION option,
    __out_bcount_part_opt(*pcbBuffer,*pcbBuffer) LPVOID pvBuffer,
    __inout ULONG *pcbBuffer );


#if defined(__cplusplus)
}
#endif

#endif //SSFSDKAPI_H
