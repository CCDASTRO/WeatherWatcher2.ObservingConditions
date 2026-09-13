Imports System
Imports System.IO
Imports System.Collections
Imports System.Runtime.InteropServices
Imports ASCOM.DeviceInterface
Imports ASCOM.Utilities

<Guid("0A886EE8-A907-4B11-8D7C-D615ECD1BF1D")>
<ClassInterface(ClassInterfaceType.None)>
<ComVisible(True)>
Public Class SafetyMonitor
    Implements ISafetyMonitorV3

#Region "Driver Constants"

    Friend Shared driverID As String =
        "ASCOM.WeatherWatcher.SafetyMonitor"

    Private Shared ReadOnly driverDescription As String =
        "WeatherWatcher SafetyMonitor"

    Friend Shared FileLocationProfileName As String =
        "Data File"

    Friend Shared TraceStateProfileName As String =
        "Trace Level"

    Friend Shared DataFileDefault As String =
        "C:\ProgramData\WeatherWatcher2\weatherdata.txt"

    Friend Shared TraceStateDefault As String =
        "False"

#End Region

#Region "Private Variables"

    Friend Shared DataFile As String
    Friend Shared traceState As Boolean

    Private connectedState As Boolean
    Private utilities As Util
    Private TL As TraceLogger

#End Region

#Region "Constructor"

    Public Sub New()

        ReadProfile()

        TL = New TraceLogger("", "WeatherWatcher")
        TL.Enabled = traceState

        TL.LogMessage("Constructor",
                      "Starting initialization")

        connectedState = False
        utilities = New Util()

        TL.LogMessage("Constructor",
                      "Initialization complete")

    End Sub

#End Region

#Region "Connection"

    Public Property Connected As Boolean _
        Implements ISafetyMonitorV3.Connected

        Get
            TL.LogMessage("Connected Get",
                          connectedState.ToString())
            Return connectedState
        End Get

        Set(value As Boolean)

            If value = connectedState Then Return

            connectedState = value

            If value Then
                TL.LogMessage("Connected Set",
                              "Connected")
            Else
                TL.LogMessage("Connected Set",
                              "Disconnected")
            End If

        End Set
    End Property

    Public ReadOnly Property Connecting As Boolean _
        Implements ISafetyMonitorV3.Connecting

        Get
            Return False
        End Get
    End Property

    Public Sub Connect() _
        Implements ISafetyMonitorV3.Connect

        Connected = True

    End Sub

    Public Sub Disconnect() _
        Implements ISafetyMonitorV3.Disconnect

        Connected = False

    End Sub

#End Region

    Friend Shared Function RainCloudRecordSafe(contents As String, nowUtc As DateTime) As Boolean
                    Dim parts = contents.Split("|"c)
                    Dim ticks As Long
                    If parts.Length <> 3 OrElse Not Long.TryParse(parts(1), Globalization.NumberStyles.None, Globalization.CultureInfo.InvariantCulture, ticks) Then Return False
                    If ticks < DateTime.MinValue.Ticks OrElse ticks > DateTime.MaxValue.Ticks Then Return False
                    Dim generated = New DateTime(ticks, DateTimeKind.Utc)
                    Dim resultAge = (nowUtc - generated).TotalSeconds
                    Return resultAge >= 0 AndAlso resultAge < 10 AndAlso parts(2) = "0"
    End Function

#Region "Safety"

    Public ReadOnly Property IsSafe As Boolean _
        Implements ISafetyMonitorV3.IsSafe

        Get
            Try
                TL.LogMessage("IsSafe",
                              "Checking safe state")

                If String.IsNullOrWhiteSpace(DataFile) Then
                    TL.LogMessage("IsSafe",
                                  "Data file path is blank")
                    Return False
                End If

                If Not File.Exists(DataFile) Then
                    TL.LogMessage("IsSafe",
                                  "Data file not found")
                    Return False
                End If

                Dim fileInfo As New FileInfo(DataFile)
                Dim age As TimeSpan =
                    DateTime.Now - fileInfo.LastWriteTime

                TL.LogMessage("IsSafe",
                              "File age = " &
                              age.TotalSeconds.ToString("F0") &
                              " seconds")

                If age.TotalSeconds > 120 Then
                    TL.LogMessage("IsSafe",
                                  "File is stale")
                    Return False
                End If

                Dim contents As String =
                    File.ReadAllText(DataFile).Trim()

                If String.IsNullOrWhiteSpace(contents) Then
                    TL.LogMessage("IsSafe",
                                  "File empty")
                    Return False
                End If

                If contents.StartsWith("WW2RC1|", StringComparison.Ordinal) Then
                    Return RainCloudRecordSafe(contents, DateTime.UtcNow)
                End If
                If contents.Contains("|") Then Return False

                Dim lastChar As Char =
                    contents(contents.Length - 1)

                TL.LogMessage("IsSafe",
                              "Last character = " &
                              lastChar)

                If lastChar = "0"c Then
                    TL.LogMessage("IsSafe",
                                  "SAFE")
                    Return True
                Else
                    TL.LogMessage("IsSafe",
                                  "UNSAFE")
                    Return False
                End If

            Catch ex As Exception

                TL.LogMessageCrLf("IsSafe Error",
                                  ex.ToString())

                Return False

            End Try
        End Get
    End Property

#End Region

#Region "Common ASCOM Properties"

    Public ReadOnly Property Name As String _
        Implements ISafetyMonitorV3.Name

        Get
            Return "WeatherWatcher Safety Monitor"
        End Get
    End Property

    Public ReadOnly Property Description As String _
        Implements ISafetyMonitorV3.Description

        Get
            Return driverDescription
        End Get
    End Property

    Public ReadOnly Property DriverInfo As String _
        Implements ISafetyMonitorV3.DriverInfo

        Get
            Return "WeatherWatcher SafetyMonitor Driver"
        End Get
    End Property

    Public ReadOnly Property DriverVersion As String _
        Implements ISafetyMonitorV3.DriverVersion

        Get
            Return Reflection.Assembly.
                GetExecutingAssembly().
                GetName().
                Version.
                ToString(2)
        End Get
    End Property

    Public ReadOnly Property InterfaceVersion As Short _
        Implements ISafetyMonitorV3.InterfaceVersion

        Get
            Return 3
        End Get
    End Property

#End Region

#Region "Required ISafetyMonitorV3 Members"

    Public Sub SetupDialog() _
        Implements ISafetyMonitorV3.SetupDialog

        Using f As New SetupDialogForm()

            If f.ShowDialog() = Windows.Forms.DialogResult.OK Then
                ReadProfile()
            End If

        End Using

    End Sub

    Public ReadOnly Property SupportedActions As ArrayList _
        Implements ISafetyMonitorV3.SupportedActions

        Get
            Return New ArrayList()
        End Get
    End Property

    Public Function Action(ActionName As String,
                           ActionParameters As String) As String _
        Implements ISafetyMonitorV3.Action

        Throw New ASCOM.ActionNotImplementedException(
            "Action " & ActionName)

    End Function

    Public Sub CommandBlind(Command As String,
                            Optional Raw As Boolean = False) _
        Implements ISafetyMonitorV3.CommandBlind

        Throw New ASCOM.MethodNotImplementedException(
            "CommandBlind")

    End Sub

    Public Function CommandBool(Command As String,
                                Optional Raw As Boolean = False) As Boolean _
        Implements ISafetyMonitorV3.CommandBool

        Throw New ASCOM.MethodNotImplementedException(
            "CommandBool")

    End Function

    Public Function CommandString(Command As String,
                                  Optional Raw As Boolean = False) As String _
        Implements ISafetyMonitorV3.CommandString

        Throw New ASCOM.MethodNotImplementedException(
            "CommandString")

    End Function

    Public ReadOnly Property DeviceState As IStateValueCollection _
    Implements ISafetyMonitorV3.DeviceState

        Get

            Dim states As New StateValueCollection()

            states.Add(
            New StateValue(
                "IsSafe",
                IsSafe))

            Return states

        End Get

    End Property

#End Region

#Region "Dispose"

    Public Sub Dispose() _
        Implements ISafetyMonitorV3.Dispose

        TL?.Dispose()
        TL = Nothing

        utilities?.Dispose()
        utilities = Nothing

    End Sub

#End Region

#Region "ASCOM Registration"

    Private Shared Sub RegUnregASCOM(ByVal register As Boolean)

        Using profile As New ASCOM.Utilities.Profile()
            profile.DeviceType = "SafetyMonitor"

            If register Then
                profile.Register(
                driverID,
                driverDescription)
            Else
                profile.Unregister(
                driverID)
            End If
        End Using

    End Sub

    <ComRegisterFunction()>
    Public Shared Sub RegisterASCOM(ByVal t As Type)
        RegUnregASCOM(True)
    End Sub

    <ComUnregisterFunction()>
    Public Shared Sub UnregisterASCOM(ByVal t As Type)
        RegUnregASCOM(False)
    End Sub

#End Region
#Region "Profile"

    Friend Sub ReadProfile()

        Using profile As New Profile()

            profile.DeviceType = "SafetyMonitor"

            traceState =
                Convert.ToBoolean(
                    profile.GetValue(
                        driverID,
                        TraceStateProfileName,
                        "",
                        TraceStateDefault))

            DataFile =
                profile.GetValue(
                    driverID,
                    FileLocationProfileName,
                    "",
                    DataFileDefault)

        End Using

    End Sub

#End Region

End Class
