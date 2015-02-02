Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Imports System.Xml
Imports UpgradeHelpers.Helpers
Public Class ETLControl
	'Constant for Module Name Used In Error Functions
	Const cModule As String = "ETLControl."

	Public ShowDebugForm As Boolean
	Public SingleStep As Boolean

	Private moXMLDoc As XmlDocument
	Private moETLConnections As ETLConnections
	'Private moActiveCommand As ETLCommand
	Private mbXMLDocLoaded As Boolean
	'Private msLocation As String


	Public Function LoadXMLConfig(ByVal XMLFileName As String, ByRef ErrorMessage As String) As Integer
		Const cProcedure As String = "LoadXMLConfig()"
		Dim lReturn As Integer = 0
		Try

			lReturn = 0
            mbXMLDocLoaded = False
            'REMOVE - UNSUPPORTED
			'UPGRADE_ISSUE: (2064) MSXML2.DOMDocument60 property moXMLDoc.validateOnParse was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
            'moXMLDoc.validateOnParse = False
			'UPGRADE_ISSUE: (2064) MSXML2.DOMDocument60 property moXMLDoc.async was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
            'moXMLDoc.async = False
			'Load XML Into Document Object
			Dim temp_xml_result As Boolean
			Try
				moXMLDoc.Load(XMLFileName)
				temp_xml_result = True

			Catch parseError As System.Exception
				temp_xml_result = False
			End Try
			If Not temp_xml_result Then
				ErrorMessage = MainErrHandler(0, "Error Loading XML File", cModule & cProcedure)
            Else
                'REMOVE - UNSUPPORTED
                'Validate XML Against Schema
                'UPGRADE_ISSUE: (2064) MSXML2.DOMDocument60 method moXMLDoc.Validate was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
                'moXMLDoc.Validate()
                'UPGRADE_ISSUE: (2064) MSXML2.DOMDocument60 property moXMLDoc.parseError was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
                'UPGRADE_ISSUE: (2064) MSXML2.IXMLDOMParseError property moXMLDoc.parseError.ErrorCode was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
                'If moXMLDoc.parseError.errorCode <> 0 Then
                'UPGRADE_ISSUE: (2064) MSXML2.DOMDocument60 property moXMLDoc.parseError was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
                'UPGRADE_ISSUE: (2064) MSXML2.IXMLDOMParseError property moXMLDoc.parseError.srcText was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
                'ErrorMessage = MainErrHandler(0, "Error Validating XML File: " & Environment.NewLine & moXMLDoc.parseError.srcText, cModule & cProcedure)
                'Else
                'Load Connections Into Collection
                If Not (moETLConnections.LoadFromXML(moXMLDoc, ErrorMessage) = 0) Then
                    'Return Success
                    mbXMLDocLoaded = True
                    lReturn = 1

                End If
            End If

        Catch excep As System.Exception

            ErrorMessage = MainErrHandler(Information.Err().Number, excep.Message, cModule & cProcedure)

        Finally
            MainDebug("Function Exit: " & lReturn, cModule & cProcedure)
        End Try
		Return lReturn

	End Function

	Public Function LoadJobStepsFromXML(ByVal JobID As String, ByRef StepList As XmlNodeList, ByRef ErrorMessage As String) As Integer
		Const cProcedure As String = "LoadJobStepsFromXML()"
		Dim lReturn, lNodeCount As Integer
		Try
			Dim oJobNode As XmlNode

			lReturn = 0
			'Locate Job Node
			oJobNode = moXMLDoc.SelectSingleNode("//root/jobs/job[@id=""" & JobID & """]")
			If oJobNode Is Nothing Then
				ErrorMessage = MainErrHandler(0, "Error, Job ID Not Found", cModule & cProcedure, "Job: " & JobID)
			Else
				'Load NodeList of Steps
				StepList = oJobNode.SelectNodes("step")
				lNodeCount = StepList.Count
				If lNodeCount < 1 Then
					ErrorMessage = MainErrHandler(0, "Error, Job Must Have At Least 1 Step Defined", cModule & cProcedure, "Job: " & JobID)
				Else
					lReturn = lNodeCount

				End If
			End If

		Catch excep As System.Exception

			ErrorMessage = MainErrHandler(Information.Err().Number, excep.Message, cModule & cProcedure)

		Finally 
			MainDebug("Function Exit: " & lReturn, cModule & cProcedure)
		End Try
		Return lReturn

	End Function

	Public Function ExecuteJobStep(ByVal JobID As String, ByRef StepNode As XmlElement, ByRef ErrorMessage As String) As Integer
		Const cProcedure As String = "ExecuteJobStep()"
		Dim lReturn As Integer = 0
		Try

			Dim oETLCommand As ETLCommand
			Dim oETLCommandNode As XmlElement
			Dim sStepName, sLocation As String

			'Locate Job Node
			sStepName = ReflectionHelper.GetPrimitiveValue(Of String)(StepNode.GetAttribute("name"))
			sLocation = "Job:  " & JobID & Environment.NewLine & "Step: " & sStepName
			oETLCommandNode = StepNode.SelectSingleNode("command")
			oETLCommand = New ETLCommand()
			If oETLCommand.LoadFromXML(oETLCommandNode, Me, sLocation, ErrorMessage) Then
				If oETLCommand.Execute(Me, ErrorMessage) Then
					lReturn = 1
				End If
			End If
			oETLCommand = Nothing

		Catch excep As System.Exception

			ErrorMessage = MainErrHandler(Information.Err().Number, excep.Message, cModule & cProcedure)

		Finally 
			MainDebug("Function Exit: " & lReturn, cModule & cProcedure)
		End Try

		Return lReturn

	End Function

	Public Function ExecuteJob(ByVal JobID As String, Optional ByVal XMLFileName As String = "", Optional ByRef ErrorMessage As String = "") As Integer
		On Error GoTo LocalErrHandler
		Const cProcedure As String = "ExecuteJob()"
		Dim oStepNodes As XmlNodeList
		Dim oStepNode As XmlElement
		Dim lStepCount, lReturn As Integer

		lReturn = 0
		If Not mbXMLDocLoaded Then
			If XMLFileName = "" Then
				ErrorMessage = MainErrHandler(0, "Must Specify XMLFileName Unless Already Loaded", cModule & cProcedure)
				GoTo LocalExit
			Else
				If LoadXMLConfig(XMLFileName, ErrorMessage) <= 0 Then
					GoTo LocalExit
				End If
			End If
		End If
		lStepCount = LoadJobStepsFromXML(JobID, oStepNodes, ErrorMessage)
		If lStepCount > 0 Then
			For I As Integer = 1 To lStepCount
				oStepNode = oStepNodes.Item(I - 1)
				lReturn = ExecuteJobStep(JobID, oStepNode, ErrorMessage)
				If lReturn <= 0 Then
					Exit For
				End If
			Next I
		End If

LocalExit:
		MainDebug("Function Exit: " & lReturn, cModule & cProcedure)
		Return lReturn

LocalErrHandler:
		ErrorMessage = MainErrHandler(Information.Err().Number, Information.Err().Description, cModule & cProcedure)
		Resume LocalExit

    End Function

	Public ReadOnly Property XMLDoc() As XmlDocument
		Get
			Return moXMLDoc
		End Get
	End Property

	Public ReadOnly Property ETLConnections() As ETLConnections
		Get
			Return moETLConnections
		End Get
	End Property


	Public Property ModalErrors() As Boolean
		Get
			Return gbModalErrors
		End Get
		Set(ByVal Value As Boolean)
			gbModalErrors = Value
		End Set
	End Property

	Public Sub New()
		MyBase.New()
		moXMLDoc = New XmlDocument()
		moETLConnections = New ETLConnections()
	End Sub

	Protected Overrides Sub Finalize()
		moXMLDoc = Nothing
		moETLConnections = Nothing
	End Sub
End Class