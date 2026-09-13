' ================================
' SetupDialogForm.vb
' Clean error-free version
' ================================

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Diagnostics
Imports ASCOM.Utilities

<ComVisible(False)>
Public Class SetupDialogForm

    Private utilities As New Util()

    Private Sub SetupDialogForm_Load(
        sender As Object,
        e As EventArgs) Handles MyBase.Load

        LoadSettings()
    End Sub

    Private Sub LoadSettings()
        Try
            Using driverProfile As New Profile()

                driverProfile.DeviceType = "SafetyMonitor"

                TextBox1.Text = driverProfile.GetValue(
                    SafetyMonitor.driverID,
                    SafetyMonitor.FileLocationProfileName,
                    String.Empty,
                    "")

                chkTrace.Checked = Convert.ToBoolean(
                    driverProfile.GetValue(
                        SafetyMonitor.driverID,
                        SafetyMonitor.TraceStateProfileName,
                        String.Empty,
                        "False"))

            End Using

        Catch ex As Exception
            MessageBox.Show(
                ex.Message,
                "Load Settings Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveSettings()
        Try
            SafetyMonitor.DataFile = TextBox1.Text.Trim()
            SafetyMonitor.traceState = chkTrace.Checked

            Using driverProfile As New Profile()

                driverProfile.DeviceType = "SafetyMonitor"

                driverProfile.WriteValue(
                    SafetyMonitor.driverID,
                    SafetyMonitor.FileLocationProfileName,
                    SafetyMonitor.DataFile)

                driverProfile.WriteValue(
                    SafetyMonitor.driverID,
                    SafetyMonitor.TraceStateProfileName,
                    SafetyMonitor.traceState.ToString())

            End Using

        Catch ex As Exception
            MessageBox.Show(
                ex.Message,
                "Save Settings Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OK_Button_Click(
        sender As Object,
        e As EventArgs) Handles OK_Button.Click

        SaveSettings()

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(
        sender As Object,
        e As EventArgs) Handles Cancel_Button.Click

        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub Button2_Click(
        sender As Object,
        e As EventArgs) Handles Button2.Click

        Try
            Using dlg As New OpenFileDialog()

                dlg.Title = "Select WeatherWatcher data file"
                dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
                dlg.InitialDirectory = "C:\ProgramData\WeatherWatcher2"
                dlg.CheckFileExists = True
                dlg.Multiselect = False

                If dlg.ShowDialog() = DialogResult.OK Then
                    TextBox1.Text = dlg.FileName
                End If

            End Using

        Catch ex As Exception
            MessageBox.Show(
                ex.Message,
                "Browse File Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Button1_Click(
        sender As Object,
        e As EventArgs) Handles Button1.Click

        Try
            If String.IsNullOrWhiteSpace(TextBox1.Text) Then
                MessageBox.Show(
                    "Please select a file first.",
                    "No File Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Exit Sub
            End If

            If Not File.Exists(TextBox1.Text) Then
                MessageBox.Show(
                    "File does not exist.",
                    "Missing File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Exit Sub
            End If

            TextBox2.Text = File.ReadAllText(TextBox1.Text)

        Catch ex As Exception
            MessageBox.Show(
                ex.Message,
                "Read File Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub



End Class
