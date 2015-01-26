Attribute VB_Name = "Globals"
Option Explicit

Global gbModalErrors As Boolean
Global gbLogDebug As Boolean

Sub MainDebug(ByVal DebugMessage As String, _
  Optional ByVal DebugSource As String = "")

  If gbLogDebug Then
    Debug.Print DebugSource & ": " & DebugMessage
  End If
  
End Sub

Function MainErrHandler(ByVal ErrorCode As Long, ByVal ErrorDescr As String, _
  ByVal ErrorSource As String, Optional ErrorLocation As String = "") As String
Dim sError As String

  sError = "Error From PauloETL Library In " & ErrorSource & vbCrLf & _
    "Error Code: " & CStr(ErrorCode) & " - " & ErrorDescr
  If ErrorLocation <> "" Then
    sError = sError & vbCrLf & "Error Location in Configuration:" & vbCrLf & ErrorLocation
  End If
  'If Modal Errors Set then Display Message Box
  If gbModalErrors Then
    MsgBox sError, vbExclamation, "ETL Error in " & ErrorSource
  End If
  MainErrHandler = sError
    
End Function

Function GetADOParamDir(ByVal sParamDir As String) As ADODB.ParameterDirectionEnum
Dim vReturn As ADODB.ParameterDirectionEnum

  Select Case sParamDir
    Case "adparaminput"
      vReturn = adParamInput
    Case "adparaminputoutput"
      vReturn = adParamInputOutput
    Case "adparamoutput"
      vReturn = adParamOutput
    Case "adparamreturnvalue"
      vReturn = adParamReturnValue
    Case Else
      vReturn = adParamUnknown
  End Select
  GetADOParamDir = vReturn
  
End Function


Function GetADODataType(ByVal sDataType As String) As ADODB.DataTypeEnum
Dim vReturn As ADODB.DataTypeEnum

  Select Case sDataType
    Case "adchar"
      vReturn = adChar
    Case "adboolean"
      vReturn = adBoolean
    Case "addate"
      vReturn = adDate
    Case "adinteger"
      vReturn = adInteger
    Case "advarchar"
      vReturn = adVarChar
    Case "adsingle"
      vReturn = adSingle
    Case "addouble"
      vReturn = adDouble
    Case Else
      vReturn = adEmpty
  End Select
  GetADODataType = vReturn
  
End Function

Function GetXmlCData(oNode As MSXML2.IXMLDOMNode) As String
Dim sData As String, I As Long, oChild As MSXML2.IXMLDOMNode

  If oNode.hasChildNodes Then
    For I = 1 To oNode.childNodes.length
    Set oChild = oNode.childNodes.Item(I)
    If oChild.nodeType = NODE_CDATA_SECTION Then
      sData = oChild.Text
      Exit For
    End If
    Next I
  End If
  GetXmlCData = sData
  
End Function


