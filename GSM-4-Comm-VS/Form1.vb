' *****************************************************************************************************
' GSM-4-CommVS 
' (c) 2017 CWE, Inc.
' 2017-12-20 v2.61
' created with Visual Basic.net 2015
' Conutrol program for CWE GSM-3 Gas Mixer
' *****************************************************************************************************
Option Explicit On

Public Class GSMTimed
    Dim Instring As String
    Dim OutputString As String
    Dim gas1, gas2, gas3, gas4, pct1HB, pct1LB, pct2HB, pct2LB, pct3HB, pct3LB, pct4HB, pct4LB, tflowHB, tflowLB As Byte
    Dim command, mix_running As Byte
    Dim counter As Long
    Dim totalpct1, totalpct2, totalpct3, totalpct4
    Dim tflow, pct1, pct2, pct3, pct4 As Integer
    Dim Temp As String
    Dim dataFileName
    Dim hLogFile, MixNo As Integer
    Dim pct1a, pct1b, pct1c, pct1d, pct2a, pct2b, pct2c, pct2d, pct3a, pct3b, pct3c, pct3d, pct4a, pct4b, pct4c, pct4d As Double
    Dim TotFlow1, TotFlow2, TotFlow3, TotFlow4
    Dim flgFlowError1, flgFlowError2, flgFlowError3, flgFlowError4, flgFlowError5
    Dim flgFlowError6, flgFlowError7, flgFlowError8, flgFlowError9, flgFlowError10
    Dim flgFlowError11, flgFlowError12, flgFlowError13
    Dim flgFlowError16, flgFlowError17, flgFlowError18, flgFlowError19
    Dim sHour, sMin, sSec As Integer        ' sequencer time variables
    Dim xb, yb, zb, flgStop As Boolean
    Dim seqRunning As Boolean = False       ' flag to prevent user from clicking any RUN buttons while sequencer is running
    Dim duration, start, finish             ' sequencer time variables
    Dim seconds_counter As Integer
    Dim version As Integer = 261            ' keep version updated!
    Dim pctGas1, pctGas2, pctGas3, pctGas4
    Dim run_time(100) As Integer
    Dim later, right_now
    Dim maxflow1, maxflow2, maxflow3, maxflow4         ' ranges of installed flow controllers
    Dim maxTotalFlow, FillGas
    Dim leds() As Button                     ' this is really a control array; elements defined in form load below
    Dim stepboxes() As TextBox               '  control array of sequencer text boxes containing run times
    Dim stepcombos() As ComboBox             '  control array of combobox for step actions



    '=========================================================================================================================================
    '                                           STARTUP ROUTINES
    '=========================================================================================================================================
    ' Load main form
    Private Sub GSMTimed_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim x As Integer
        On Error Resume Next
        GetSerialPortNames()                    ' show available serial port names in combobox
        Call LoadConfigurationToolStripMenuItem_Click(sender, e)

        btnFillGas0.Checked = True              ' select fill gas to be gas 1 (usually Nitrogen)
        btnFillGas1.Checked = False             ' de-select others
        btnFillGas2.Checked = False
        btnFillGas3.Checked = False

        '*************************************************************************************************************************************
        ' create control arrays for the SEQUENCER; addressed by: leds(index), stepboxes(index), stepcombos(index), index is 0-14 (for steps 1-15)
        leds = New Button() {ledStep1, ledStep2, ledStep3, ledStep4, ledStep5, ledStep6, ledStep7, ledStep8, ledStep9, ledStep10,
           ledStep11, ledStep12, ledStep13, ledStep14, ledStep15}

        stepboxes = New TextBox() {tbxStep1, tbxStep2, tbxStep3, tbxStep4, tbxStep5, tbxStep6, tbxStep7, tbxStep8,
            tbxStep9, tbxStep10, tbxStep11, tbxStep12, tbxStep13, tbxStep14, tbxStep15}

        stepcombos = New ComboBox() {ComboBox1, ComboBox2, ComboBox3, ComboBox4, ComboBox5, ComboBox6, ComboBox7, ComboBox8, ComboBox9,
            ComboBox10, ComboBox11, ComboBox12, ComboBox13, ComboBox14, ComboBox15}
        '*************************************************************************************************************************************

        ' initialize colors of LEDs
        For x = 0 To 14
            leds(x).BackColor = Color.RosyBrown
        Next

        CreateChart()
    End Sub

    Private Sub stepTimer_Tick(sender As Object, e As EventArgs) Handles stepTimer.Tick
        seconds_counter = seconds_counter + 1
    End Sub


    ' load and update mixture BAR CHART
    ' this is called whenever the running gas mixture is changed
    Private Sub CreateChart()
        Me.Chart2.DataSource = GetData()
        Me.Chart2.Series.Clear()
        Chart2.ChartAreas.Clear()
        Chart2.ChartAreas.Add("Area0")
        Me.Chart2.Series.Add("GAS1")    ' Math

        Chart2.Series(0).XValueMember = "Name"
        Chart2.Series(0).YValueMembers = "Percent"
        Chart2.Series(0).IsValueShownAsLabel = True
        Chart2.ChartAreas(0).AxisX.LabelStyle.Angle = 0 '-90

        Chart2.ChartAreas(0).AxisX.MajorGrid.Enabled = False
        Chart2.ChartAreas(0).AxisY.MajorGrid.Enabled = True
        Chart2.ChartAreas(0).AxisY.MajorGrid.LineColor = Color.LightGray

    End Sub


    ' This function gets the data to put into the BAR CHART above
    Public Function GetData() As DataTable
        Dim dt = New DataTable()
        dt.Columns.Add("Name", GetType(String))
        dt.Columns.Add("Percent", GetType(Integer))

        Select Case mix_running
            Case 0
                pctGas1 = 25.0
                pctGas2 = 25.0
                pctGas3 = 25.0
                pctGas4 = 25.0
            Case 1
                pctGas1 = Val(tbxPctA1.Text)
                pctGas2 = Val(tbxPctB1.Text)
                pctGas3 = Val(tbxPctC1.Text)
                pctGas4 = Val(tbxPctD1.Text)
            Case 2
                pctGas1 = Val(tbxPctA2.Text)
                pctGas2 = Val(tbxPctB2.Text)
                pctGas3 = Val(tbxPctC2.Text)
                pctGas4 = Val(tbxPctD2.Text)
            Case 3
                pctGas1 = Val(tbxPctA3.Text)
                pctGas2 = Val(tbxPctB3.Text)
                pctGas3 = Val(tbxPctC3.Text)
                pctGas4 = Val(tbxPctD3.Text)
            Case 4
                pctGas1 = Val(tbxPctA4.Text)
                pctGas2 = Val(tbxPctB4.Text)
                pctGas3 = Val(tbxPctC4.Text)
                pctGas4 = Val(tbxPctD4.Text)
        End Select

        dt.Rows.Add("GAS1", pctGas1)   ' alex
        dt.Rows.Add("GAS2", pctGas2)  'richard
        dt.Rows.Add("GAS3", pctGas3)    ' alice
        dt.Rows.Add("GAS4", pctGas4)    ' alice 
        Return dt
    End Function


    ' this finds all the available SERIAL PORTS and puts them into the combo control
    Sub GetSerialPortNames()
        For Each sp As String In My.Computer.Ports.SerialPortNames ' Show all available COM ports.
            comboPort.Items.Add(sp)
            comboPort.SelectedIndex = 0     ' point to first valid port on the list
            comboPort.GetItemText(0)        ' pre-load this into the selection textbox
        Next
    End Sub


    ' update MESSAGE BOX with any error messages
    Private Sub tbxErrMsg_TextChanged(sender As Object, e As EventArgs) Handles tbxErrMsg.TextChanged
        If flgFlowError1 = 1 Or flgFlowError2 = 1 Or flgFlowError3 = 1 Or flgFlowError4 = 1 Or
            flgFlowError5 = 1 Or flgFlowError6 = 1 Or flgFlowError7 = 1 Or flgFlowError8 = 1 Or
            flgFlowError9 = 1 Or flgFlowError10 = 1 Or flgFlowError11 = 1 Or flgFlowError12 = 1 Then
            tbxErrMsg.Text = "Flow out of range!" & vbCrLf & "Adjust gas percent or TOTAL FLOW."
        ElseIf flgFlowError16 = 1 Or flgFlowError17 = 1 Or flgFlowError18 = 1 Or flgFlowError19 = 1 Then
            tbxErrMsg.Text = "ERROR! TOTAL PERCENT must be 100%" & vbCrLf & "Adjust non-fill gas percents"
        Else
            tbxErrMsg.Text = "STATUS: OK"
        End If
    End Sub



    ' this is the ProgBar1 progress bar timer
    ' tick increment is set programatically to allow 100 ticks full scale
    Private Sub progTimer_Tick(sender As Object, e As EventArgs) Handles progTimer.Tick
        If ProgBar1.Value < ProgBar1.Maximum Then
            ProgBar1.Value += 1     ' increment progress bar at bottom of screen
        End If
    End Sub


    ' below are attempts to allow user to directly enter mix percent values in textbox...  so far, not good!

    'Private Sub tbxPctA1_TextChanged(sender As Object, e As EventArgs) Handles tbxPctA1.TextChanged
    '    upDnPctA1.Focus()
    '    upDnPctA1.Value = -1 * Val(tbxPctA1.Text) * 10

    'End Sub

    'Private Sub tbxPctB1_TextChanged(sender As Object, e As EventArgs) Handles tbxPctB1.TextChanged
    '    upDnPctB1.Value = -1 * Val(tbxPctB1.Text) * 10
    'End Sub

    'Private Sub tbxPctC1_TextChanged(sender As Object, e As EventArgs) Handles tbxPctC1.TextChanged
    '    upDnPctC1.Value = -1 * Val(tbxPctC1.Text) * 10
    'End Sub


    '=========================================================================================================================================
    '                             USER HAS UPDATED FLOW CONTROLLER CONFIGURATION
    '=========================================================================================================================================
    Private Sub tbxMaxFlow2_TextChanged(sender As Object, e As EventArgs) Handles tbxMaxFlow2.TextChanged
        maxflow2 = Val(tbxMaxFlow2.Text)
    End Sub

    Private Sub tbxMaxFlow3_TextChanged(sender As Object, e As EventArgs) Handles tbxMaxFlow3.TextChanged
        maxflow3 = Val(tbxMaxFlow3.Text)
    End Sub

    Private Sub tbxMaxFlow1_TextChanged(sender As Object, e As EventArgs) Handles tbxMaxFlow1.TextChanged
        maxflow1 = Val(tbxMaxFlow1.Text)
    End Sub
    Private Sub tbxMaxFlow4_TextChanged(sender As Object, e As EventArgs) Handles tbxMaxFlow4.TextChanged
        maxflow4 = Val(tbxMaxFlow4.Text)
    End Sub



    '=========================================================================================================================================
    '                                           RUN SEQUENCER
    '=========================================================================================================================================
    Private Sub btnRunSequence_Click(sender As Object, e As EventArgs) Handles btnRunSequence.Click
        Dim x As Integer
        flgStop = False                                 ' flag is set by clicking HALT SEQUENCE

        If seqRunning = True Then Return

        seqRunning = True                               ' flag to prevent user from clicking other RUN buttons while sequencer is running

restart:                                                ' cycle back here to repeat sequence
        ' check user time boxes for proper format "HH:MM:SS",  highlight with red text if invalid
        For x = 0 To 14
            If Len(stepboxes(x).Text) <> 8 Then
                stepboxes(x).ForeColor = Color.Red ' check for proper string format
                stepboxes(x).Refresh()
            Else
                stepboxes(x).ForeColor = Color.Black
                stepboxes(x).Refresh()
            End If
        Next

        '---------------------------------------------------------------------------------------------------------------------------------------------
        For x = 0 To 14
            If stepcombos(x).Text = "NONE" Then GoTo skip_this_step ' NONE, so skip this step
            If stepcombos(x).Text = "REPEAT" Then GoTo restart      ' REPEAT, so go back to top and repeat sequence
            If stepcombos(x).Text = "STOP" Then Exit For            ' STOP, so clean up and get out

            ' this must be a timed run
            ' check for proper format HH:MM:SS
            If Len(stepboxes(x).Text) <> 8 Then
                stepboxes(x).ForeColor = Color.Red                  ' check for proper string format
            Else
                stepboxes(x).ForeColor = Color.Black
            End If

            ' extract time from string
            sSec = Val((stepboxes(x).Text).Substring(6, 2))         ' extract seconds from string
            If sSec > 59 Then sSec = 59
            sMin = Val((stepboxes(x).Text).Substring(3, 2))         ' extract minutes
            If sMin > 59 Then sMin = 59
            sHour = Val((stepboxes(x).Text).Substring(0, 2))        ' extract hours
            duration = sSec + (60 * sMin) + (3600 * sHour)          ' convert all to seconds
            If duration = 0 Then GoTo skip_this_step                ' prevent zero time values

            '------------------------------------------------------------------------------------------------------------------------------------------
            Select Case stepcombos(x).Text
                Case "Mix 1"                        ' run MIX 1
                    progTimer.Interval = Int(duration * 1000 / 100) ' set interval to give 100 ticks of progress bar
                    ProgBar1.Value = 0              ' reset progress bar to zero
                    progTimer.Enabled = True
                    progTimer.Start()
                    mix_running = 1
                    ledRunning1.BackColor = Color.Red
                    ledRunning1.Refresh()
                    Call btnRun1_Click(sender, e)   ' fire this button to run selected mix
                    leds(x).BackColor = Color.Red
                    leds(x).Refresh()

                    btnHaltSequence.Focus()         ' this makes sure the halt sequence button gets read all the time

                    stepTimer.Start()               ' 
                    seconds_counter = 0
                    Do While (duration > seconds_counter)
                        My.Application.DoEvents()       ' be sure background ui events are looked at
                    Loop

                    'start = Microsoft.VisualBasic.DateAndTime.Timer
                    'Do While (Microsoft.VisualBasic.DateAndTime.Timer < (start + duration)) And flgStop = False ' run time for this step
                    '    My.Application.DoEvents()       ' be sure background ui events are looked at
                    'Loop

                    leds(x).BackColor = Color.RosyBrown
                    leds(x).Refresh()
                    ProgBar1.Value = 0
                    progTimer.Enabled = False
                    If flgStop = True Then GoTo cleanup_and_exit

                Case "Mix 2"                                    ' run MIX 2
                    progTimer.Interval = Int(duration * 1000 / 100) ' set interval to give 100 ticks of progress bar
                    ProgBar1.Value = 0              ' reset progress bar to zero
                    progTimer.Enabled = True
                    progTimer.Start()
                    mix_running = 2
                    ledRunning2.BackColor = Color.Red
                    ledRunning2.Refresh()
                    Call btnRun2_Click(sender, e)   ' fire this button to run selected mix
                    leds(x).BackColor = Color.Red
                    leds(x).Refresh()

                    btnHaltSequence.Focus()         ' this makes sure the halt sequence button gets read all the time
                    stepTimer.Start()               ' 
                    seconds_counter = 0
                    Do While (duration > seconds_counter)
                        My.Application.DoEvents()       ' be sure background ui events are looked at
                    Loop

                    'start = Microsoft.VisualBasic.DateAndTime.Timer
                    'Do While (Microsoft.VisualBasic.DateAndTime.Timer < (start + duration)) And flgStop = False ' run time for this step
                    '    My.Application.DoEvents()       ' be sure background ui events are looked at
                    'Loop

                    leds(x).BackColor = Color.RosyBrown
                    leds(x).Refresh()
                    ProgBar1.Value = 0
                    progTimer.Enabled = False
                    If flgStop = True Then GoTo cleanup_and_exit

                Case "Mix 3"                                    ' run MIX 3
                    progTimer.Interval = Int(duration * 1000 / 100) ' set interval to give 100 ticks of progress bar
                    ProgBar1.Value = 0              ' reset progress bar to zero
                    progTimer.Enabled = True
                    progTimer.Start()
                    mix_running = 3
                    ledRunning3.BackColor = Color.Red
                    ledRunning3.Refresh()
                    Call btnRun3_Click(sender, e)   ' fire this button to run selected mix
                    leds(x).BackColor = Color.Red
                    leds(x).Refresh()

                    btnHaltSequence.Focus()         ' this makes sure the halt sequence button gets read all the time
                    stepTimer.Start()               ' 
                    seconds_counter = 0
                    Do While (duration > seconds_counter)
                        My.Application.DoEvents()       ' be sure background ui events are looked at
                    Loop

                    'start = Microsoft.VisualBasic.DateAndTime.Timer
                    'Do While (Microsoft.VisualBasic.DateAndTime.Timer < (start + duration)) And flgStop = False ' run time for this step
                    '    My.Application.DoEvents()       ' be sure background ui events are looked at
                    'Loop

                    leds(x).BackColor = Color.RosyBrown
                    leds(x).Refresh()
                    ProgBar1.Value = 0
                    progTimer.Enabled = False
                    If flgStop = True Then GoTo cleanup_and_exit

                Case "Mix 4"                                    ' run MIX 4
                    progTimer.Interval = Int(duration * 1000 / 100) ' set interval to give 100 ticks of progress bar
                    ProgBar1.Value = 0              ' reset progress bar to zero
                    progTimer.Enabled = True
                    progTimer.Start()
                    mix_running = 4
                    ledRunning4.BackColor = Color.Red
                    ledRunning4.Refresh()
                    Call btnRun4_Click(sender, e)   ' fire this button to run selected mix
                    leds(x).BackColor = Color.Red
                    leds(x).Refresh()

                    btnHaltSequence.Focus()         ' this makes sure the halt sequence button gets read all the time
                    stepTimer.Start()               ' 
                    seconds_counter = 0
                    Do While (duration > seconds_counter)
                        My.Application.DoEvents()       ' be sure background ui events are looked at
                    Loop

                    'start = Microsoft.VisualBasic.DateAndTime.Timer
                    'Do While (Microsoft.VisualBasic.DateAndTime.Timer < (start + duration)) And flgStop = False ' run time for this step
                    '    My.Application.DoEvents()       ' be sure background ui events are looked at
                    'Loop

                    leds(x).BackColor = Color.RosyBrown
                    leds(x).Refresh()
                    ProgBar1.Value = 0
                    progTimer.Enabled = False
                    If flgStop = True Then GoTo cleanup_and_exit
            End Select
skip_this_step:
        Next

cleanup_and_exit:       ' close down sequencer operations
        For x = 0 To 14
            leds(x).BackColor = Color.RosyBrown
        Next
        Call btnStopAll_Click(sender, e)
        seqRunning = False
    End Sub


    '=========================================================================================================================================
    '                               TOTAL FLOW SETTING ROUTINES
    '=========================================================================================================================================
    ' following subs handle all the TOTAL FLOW updowns for the 4 mixtures
    ' This was not needed in the VBC version, which used "buddy" controls
    ' NOTE: upDn's work in reverse, so values are correctred to increment with up arrow, and vice-versa
    Private Sub upDnFlow1_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnFlow1.Scroll
        tbxTFlow1.Text = -1 * upDnFlow1.Value
    End Sub
    Private Sub upDnFlow2_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnFlow2.Scroll
        tbxTFlow2.Text = -1 * upDnFlow2.Value
    End Sub
    Private Sub upDnFlow3_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnFlow3.Scroll
        tbxTFlow3.Text = -1 * upDnFlow3.Value
    End Sub
    Private Sub upDnFlow4_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnFlow4.Scroll
        tbxTFlow4.Text = -1 * upDnFlow4.Value
    End Sub




    '=========================================================================================================================================
    '                                           RUN MIXTURE PUSHBUTTON ROUTINES
    '=========================================================================================================================================
    ' following subs handle user pressing a RUN MIXTURE button for the 4 mixtures
    Private Sub btnRun1_Click(sender As Object, e As EventArgs) Handles btnRun1.Click

        If seqRunning = True Then           ' this locks out user from clicking RUN when sequencer is running
            btnRun1.Enabled = False
        Else btnRun1.enabled = True
        End If

        MSComm1.PortName = comboPort.Text
        MSComm1.Open()              ' Open the port.
        OutputString = "1"
        MSComm1.Write(OutputString)       ' send the command string
        MSComm1.Close()           ' Close the port.
        mix_running = 1
        CreateChart()                   ' update bar chart with gas percentages
    End Sub

    Private Sub btnRun2_Click(sender As Object, e As EventArgs) Handles btnRun2.Click

        If seqRunning = True Then           ' this locks out user from clicking RUN when sequencer is running
            btnRun2.Enabled = False
        Else btnRun2.Enabled = True
        End If

        MSComm1.PortName = comboPort.Text
        'MSComm1.Settings = "19200,N,8,1"
        MSComm1.Open()              ' Open the port.
        OutputString = "2"
        MSComm1.Write(OutputString)       ' send the command string
        MSComm1.Close()           ' Close the port.
        mix_running = 2
        CreateChart()                   ' update bar chart with gas percentages
    End Sub

    Private Sub btnRun3_Click(sender As Object, e As EventArgs) Handles btnRun3.Click

        If seqRunning = True Then           ' this locks out user from clicking RUN when sequencer is running
            btnRun3.Enabled = False
        Else btnRun3.Enabled = True
        End If

        MSComm1.PortName = comboPort.Text
        'MSComm1.Settings = "19200,N,8,1"
        MSComm1.Open()              ' Open the port.
        OutputString = "3"
        MSComm1.Write(OutputString)       ' send the command string
        MSComm1.Close()           ' Close the port.
        mix_running = 3
        CreateChart()                   ' update bar chart with gas percentages
    End Sub

    Private Sub btnRun4_Click(sender As Object, e As EventArgs) Handles btnRun4.Click

        If seqRunning = True Then           ' this locks out user from clicking RUN when sequencer is running
            btnRun4.Enabled = False
        Else btnRun4.Enabled = True
        End If

        MSComm1.PortName = comboPort.Text
        'MSComm1.Settings = "19200,N,8,1"
        MSComm1.Open()              ' Open the port.
        OutputString = "4"
        MSComm1.Write(OutputString)       ' send the command string
        MSComm1.Close()           ' Close the port.
        mix_running = 4
        CreateChart()                   ' update bar chart with gas percentages
    End Sub
    '-----------------------------------------------------------------------------------------------------------------------------------------
    Private Sub btnStopAll_Click(sender As Object, e As EventArgs) Handles btnStopAll.Click
        MSComm1.PortName = comboPort.Text
        MSComm1.Open()              ' Open the port.
        OutputString = "9"          ' command to stop all channels
        MSComm1.Write(OutputString) ' send the command string
        MSComm1.Close()             ' Close the port.
        mix_running = 0
        CreateChart()                   ' update bar chart with gas percentages
        Call btnHaltSequence_Click(sender, e)
    End Sub



    '=========================================================================================================================================
    '                                     STORE/RUN BUTTON ROUTINES
    '=========================================================================================================================================
    Private Sub btnStore1_Click(sender As Object, e As EventArgs) Handles btnStore1.Click
        ' construct output program string (12 bytes):
        ' 3-CHAN VERSION: MixNo,Gas1,Pct1.hb,Pct1.lb,Gas2,Pct2.hb,Pct2.lb,Gas3,Pct3.hb,Pct3.lb,TotFlow.hb,Totflow.lb
        ' 4-CHAN VERSION: MixNo,Gas1,Pct1.hb,Pct1.lb,Gas2,Pct2.hb,Pct2.lb,Gas3,Pct3.hb,Pct3.lb,Pct4.hb,Pct4.lb,TotFlow.hb,Totflow.lb

        If seqRunning = True Then Return          ' this locks out user from clicking RUN when sequencer is running

        gas1 = ComboGas0.SelectedIndex + 1
        If gas1 = 0 Then gas1 = 1   ' if not set, make it AIR
        pct1 = Val(tbxPctA1.Text) * 10
        pct1HB = Int(pct1 / 256)
        pct1LB = Int(pct1 - (Int(pct1HB) * 256))

        gas2 = ComboGas1.SelectedIndex + 1
        If gas2 = 0 Then gas2 = 1   ' if not set, make it AIR
        pct2 = Val(tbxPctB1.Text) * 10
        pct2HB = Int(pct2 / 256)
        pct2LB = Int(pct2 - (Int(pct2HB) * 256))

        gas3 = ComboGas2.SelectedIndex + 1
        If gas3 = 0 Then gas3 = 1   ' if not set, make it AIR
        pct3 = Val(tbxPctC1.Text) * 10
        pct3HB = Int(pct3 / 256)
        pct3LB = Int(pct3 - (Int(pct3HB) * 256))

        gas4 = ComboGas3.SelectedIndex + 1
        If gas4 = 0 Then gas4 = 1   ' if not set, make it AIR
        pct4 = Val(tbxPctD1.Text) * 10
        pct4HB = Int(pct4 / 256)
        pct4LB = Int(pct4 - (Int(pct4HB) * 256))

        tflow = Val(tbxTFlow1.Text)
        tflowHB = Int(tflow / 256)
        tflowLB = Int(tflow - (tflowHB * 256))

        ' send output as array of bytes
        Dim bytes() As Byte = {1, gas1, pct1HB, pct1LB, gas2, pct2HB, pct2LB, gas3, pct3HB, pct3LB, gas4, pct4HB, pct4LB, tflowHB, tflowLB}
        MSComm1.PortName = comboPort.Text
        MSComm1.Open()                  ' Open the port.
        MSComm1.Write(bytes, 0, bytes.Length)
        MSComm1.Close()                 ' Close the port.
        mix_running = 1
        CreateChart()

        'Debug.Print(1, gas1, pct1HB, pct1LB, gas2, pct2HB, pct2LB, gas3, pct3HB, pct3LB, gas4, pct4HB, pct4LB, tflowHB, tflowLB)
    End Sub

    Private Sub btnStore2_Click(sender As Object, e As EventArgs) Handles btnStore2.Click
        ' construct output program string (12 bytes):
        ' MixNo,Gas1,Pct1.hb,Pct1.lb,Gas2,Pct2.hb,Pct2.lb,Gas3,Pct3.hb,Pct3.lb,TotFlow.hb,Totflow.lb

        If seqRunning = True Then Return    ' this locks out user from clicking RUN when sequencer is running

        gas1 = ComboGas0.SelectedIndex + 1
        If gas1 = 0 Then gas1 = 1   ' if not set, make it AIR
        pct1 = Val(tbxPctA2.Text) * 10
        pct1HB = Int(pct1 / 256)
        pct1LB = Int(pct1 - (Int(pct1HB) * 256))

        gas2 = ComboGas1.SelectedIndex + 1
        If gas2 = 0 Then gas2 = 1   ' if not set, make it AIR
        pct2 = Val(tbxPctB2.Text) * 10
        pct2HB = Int(pct2 / 256)
        pct2LB = Int(pct2 - (Int(pct2HB) * 256))

        gas3 = ComboGas2.SelectedIndex + 1
        If gas3 = 0 Then gas3 = 1   ' if not set, make it AIR
        pct3 = Val(tbxPctC2.Text) * 10
        pct3HB = Int(pct3 / 256)
        pct3LB = Int(pct3 - (Int(pct3HB) * 256))

        gas4 = ComboGas3.SelectedIndex + 1
        If gas4 = 0 Then gas4 = 1   ' if not set, make it AIR
        pct4 = Val(tbxPctD2.Text) * 10
        pct4HB = Int(pct4 / 256)
        pct4LB = Int(pct4 - (Int(pct4HB) * 256))

        tflow = Val(tbxTFlow2.Text)
        tflowHB = Int(tflow / 256)
        tflowLB = Int(tflow - (tflowHB * 256))

        ' send output as array of bytes
        Dim bytes() As Byte = {2, gas1, pct1HB, pct1LB, gas2, pct2HB, pct2LB, gas3, pct3HB, pct3LB, gas4, pct4HB, pct4LB, tflowHB, tflowLB}
        MSComm1.PortName = comboPort.Text
        MSComm1.Open()                  ' Open the port.
        MSComm1.Write(bytes, 0, bytes.Length)
        MSComm1.Close()                 ' Close the port.
        mix_running = 2
        CreateChart()
    End Sub

    Private Sub btnStore3_Click(sender As Object, e As EventArgs) Handles btnStore3.Click
        ' construct output program string (12 bytes):
        ' MixNo,Gas1,Pct1.hb,Pct1.lb,Gas2,Pct2.hb,Pct2.lb,Gas3,Pct3.hb,Pct3.lb,TotFlow.hb,Totflow.lb

        If seqRunning = True Then Return          ' this locks out user from clicking RUN when sequencer is running

        gas1 = ComboGas0.SelectedIndex + 1
        If gas1 = 0 Then gas1 = 1   ' if not set, make it AIR
        pct1 = Val(tbxPctA3.Text) * 10
        pct1HB = Int(pct1 / 256)
        pct1LB = Int(pct1 - (Int(pct1HB) * 256))

        gas2 = ComboGas1.SelectedIndex + 1
        If gas2 = 0 Then gas2 = 1   ' if not set, make it AIR
        pct2 = Val(tbxPctB3.Text) * 10
        pct2HB = Int(pct2 / 256)
        pct2LB = Int(pct2 - (Int(pct2HB) * 256))

        gas3 = ComboGas2.SelectedIndex + 1
        If gas3 = 0 Then gas3 = 1   ' if not set, make it AIR
        pct3 = Val(tbxPctC3.Text) * 10
        pct3HB = Int(pct3 / 256)
        pct3LB = Int(pct3 - (Int(pct3HB) * 256))

        gas4 = ComboGas3.SelectedIndex + 1
        If gas4 = 0 Then gas4 = 1   ' if not set, make it AIR
        pct4 = Val(tbxPctD3.Text) * 10
        pct4HB = Int(pct4 / 256)
        pct4LB = Int(pct4 - (Int(pct4HB) * 256))

        tflow = Val(tbxTFlow3.Text)
        tflowHB = Int(tflow / 256)
        tflowLB = Int(tflow - (tflowHB * 256))

        ' send output as array of bytes
        Dim bytes() As Byte = {3, gas1, pct1HB, pct1LB, gas2, pct2HB, pct2LB, gas3, pct3HB, pct3LB, gas4, pct4HB, pct4LB, tflowHB, tflowLB}
        MSComm1.PortName = comboPort.Text
        MSComm1.Open()                  ' Open the port.
        MSComm1.Write(bytes, 0, bytes.Length)
        MSComm1.Close()
        mix_running = 3
        CreateChart()
    End Sub

    Private Sub btnStore4_Click(sender As Object, e As EventArgs) Handles btnStore4.Click
        ' construct output program string (12 bytes):
        ' MixNo,Gas1,Pct1.hb,Pct1.lb,Gas2,Pct2.hb,Pct2.lb,Gas3,Pct3.hb,Pct3.lb,TotFlow.hb,Totflow.lb

        If seqRunning = True Then Return          ' this locks out user from clicking RUN when sequencer is running

        gas1 = ComboGas0.SelectedIndex + 1
        If gas1 = 0 Then gas1 = 1   ' if not set, make it AIR
        pct1 = Val(tbxPctA4.Text) * 10
        pct1HB = Int(pct1 / 256)
        pct1LB = Int(pct1 - (Int(pct1HB) * 256))

        gas2 = ComboGas1.SelectedIndex + 1
        If gas2 = 0 Then gas2 = 1   ' if not set, make it AIR
        pct2 = Val(tbxPctB4.Text) * 10
        pct2HB = Int(pct2 / 256)
        pct2LB = Int(pct2 - (Int(pct2HB) * 256))

        gas3 = ComboGas2.SelectedIndex + 1
        If gas3 = 0 Then gas3 = 1   ' if not set, make it AIR
        pct3 = Val(tbxPctC4.Text) * 10
        pct3HB = Int(pct3 / 256)
        pct3LB = Int(pct3 - (Int(pct3HB) * 256))

        gas4 = ComboGas3.SelectedIndex + 1
        If gas4 = 0 Then gas4 = 1   ' if not set, make it AIR
        pct4 = Val(tbxPctD4.Text) * 10
        pct4HB = Int(pct4 / 256)
        pct4LB = Int(pct4 - (Int(pct4HB) * 256))

        tflow = Val(tbxTFlow4.Text)
        tflowHB = Int(tflow / 256)
        tflowLB = Int(tflow - (tflowHB * 256))

        ' send output as array of bytes
        Dim bytes() As Byte = {4, gas1, pct1HB, pct1LB, gas2, pct2HB, pct2LB, gas3, pct3HB, pct3LB, gas4, pct4HB, pct4LB, tflowHB, tflowLB}
        MSComm1.PortName = comboPort.Text
        MSComm1.Open()                  ' Open the port.
        MSComm1.Write(bytes, 0, bytes.Length)
        MSComm1.Close()
        mix_running = 4
        CreateChart()
    End Sub



    '=========================================================================================================================================
    '                                     PERCENTAGE UPDOWNS FOR 4 MIXTURES, 4 GASSES EACH
    '=========================================================================================================================================
    ' NOTE: upDn's work in reverse, so values are corrected to increment with up arrow, and vice-versa

    Private Sub upDnPctA1_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctA1.Scroll
        tbxPctA1.Text = -1 * upDnPctA1.Value / 10
    End Sub

    Private Sub upDnPctB1_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctB1.Scroll
        'Debug.Print(upDnPctB1.Value)
        tbxPctB1.Text = -1 * upDnPctB1.Value / 10   '**************************************************
    End Sub

    Private Sub upDnPctC1_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctC1.Scroll
        tbxPctC1.Text = -1 * upDnPctC1.Value / 10
    End Sub

    Private Sub upDnPctD1_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctD1.Scroll
        tbxPctD1.Text = -1 * upDnPctD1.Value / 10
    End Sub



    Private Sub upDnPctA2_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctA2.Scroll
        tbxPctA2.Text = -1 * upDnPctA2.Value / 10
    End Sub

    Private Sub upDnPctB2_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctB2.Scroll
        tbxPctB2.Text = -1 * upDnPctB2.Value / 10
    End Sub

    Private Sub upDnPctC2_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctC2.Scroll
        tbxPctC2.Text = -1 * upDnPctC2.Value / 10
    End Sub

    Private Sub upDnPctD2_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctD2.Scroll
        tbxPctD2.Text = -1 * upDnPctD2.Value / 10
    End Sub


    Private Sub upDnPctA3_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctA3.Scroll
        tbxPctA3.Text = -1 * upDnPctA3.Value / 10
    End Sub

    Private Sub upDnPctB3_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctB3.Scroll
        tbxPctB3.Text = -1 * upDnPctB3.Value / 10
    End Sub

    Private Sub upDnPctC3_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctC3.Scroll
        tbxPctC3.Text = -1 * upDnPctC3.Value / 10
    End Sub
    Private Sub upDnPctD3_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctD3.Scroll
        tbxPctD3.Text = -1 * upDnPctD3.Value / 10
    End Sub


    Private Sub upDnPctA4_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctA4.Scroll
        tbxPctA4.Text = -1 * upDnPctA4.Value / 10
    End Sub

    Private Sub upDnPctB4_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctB4.Scroll
        tbxPctB4.Text = -1 * upDnPctB4.Value / 10
    End Sub

    Private Sub upDnPctC4_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctC4.Scroll
        tbxPctC4.Text = -1 * upDnPctC4.Value / 10
    End Sub
    Private Sub upDnPctD4_Scroll(sender As Object, e As ScrollEventArgs) Handles upDnPctD4.Scroll
        tbxPctD4.Text = -1 * upDnPctD4.Value / 10
    End Sub




    '=========================================================================================================================================
    '                                   EXIT BUTTON AND MENU HANDLING ROUTINES
    '=========================================================================================================================================
    ' EXIT button pressed
    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        MSComm1.Close()             ' close the port
        flgStop = True
        progTimer.Enabled = False
        Call SaveConfigurationToolStripMenuItem_Click(sender, e)     ' prompt user to save configuration file
        Me.Close()                  ' close window and application
    End Sub



    '=========================================================================================================================================
    '                                    MENU LOAD CONFIGURATION FILE is selected
    '=========================================================================================================================================
    Private Sub LoadConfigurationToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadConfigurationToolStripMenuItem.Click
        Dim replace
        Dim InputString As String
        Dim fnConfig As String
        On Error Resume Next

        Dim openFileDialog1 As New OpenFileDialog()
        openFileDialog1.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
        openFileDialog1.Title = "Load Configuration File"

        If openFileDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            fnConfig = openFileDialog1.FileName ' selected file

            FileOpen(1, fnConfig, OpenMode.Input)
            Input(1, gas1)
            Input(1, gas2)
            Input(1, gas3)
            Input(1, gas4)
            Input(1, pct1a)
            Input(1, pct1b)
            Input(1, pct1c)
            Input(1, pct1d)
            Input(1, TotFlow1)

            Input(1, pct2a)
            Input(1, pct2b)
            Input(1, pct2c)
            Input(1, pct2d)
            Input(1, TotFlow2)

            Input(1, pct3a)
            Input(1, pct3b)
            Input(1, pct3c)
            Input(1, pct3d)
            Input(1, TotFlow3)

            Input(1, pct4a)
            Input(1, pct4b)
            Input(1, pct4c)
            Input(1, pct4d)
            Input(1, TotFlow4)

            Input(1, maxflow1)
            Input(1, maxflow2)
            Input(1, maxflow3)
            Input(1, maxflow4)
            Input(1, FillGas)
            FileClose(1)

            Debug.Print(maxflow1, maxflow2, maxflow3, maxflow4, FillGas)
            Debug.Print(maxflow2) '
            Debug.Print(maxflow3)
            Debug.Print(maxflow4)

            If FillGas = 0 Then
                btnFillGas0.Checked = True
                btnFillGas1.Checked = False
                btnFillGas2.Checked = False
                btnFillGas3.Checked = False
            End If

            If FillGas = 1 Then
                btnFillGas0.Checked = False
                btnFillGas1.Checked = True
                btnFillGas2.Checked = False
                btnFillGas3.Checked = False
            End If

            If FillGas = 2 Then
                btnFillGas0.Checked = False
                btnFillGas1.Checked = False
                btnFillGas2.Checked = True
                btnFillGas3.Checked = False
            End If

            If FillGas = 3 Then
                btnFillGas0.Checked = False
                btnFillGas1.Checked = False
                btnFillGas2.Checked = False
                btnFillGas3.Checked = True
            End If

            tbxMaxFlow1.Text = maxflow1     ' installed flow controllers
            tbxMaxFlow2.Text = maxflow2
            tbxMaxFlow3.Text = maxflow3
            tbxMaxFlow4.Text = maxflow4

            ComboGas0.Text = ComboGas0.Items(gas1)    ' gasses
            ComboGas1.Text = ComboGas1.Items(gas2)
            ComboGas2.Text = ComboGas2.Items(gas3)
            ComboGas3.Text = ComboGas3.Items(gas4)

            ' following values are all reversed to fit into updn controls requiring '0 - -1000' values 
            upDnPctA1.Value = -1 * pct1a         ' Mix 1, gas A
            upDnPctB1.Value = -1 * pct1b         ' Mix 1, gas B
            upDnPctC1.Value = -1 * pct1c         ' Mix 1, gas C
            upDnPctD1.Value = -1 * pct1d         ' Mix 1, gas D
            tbxTFlow1.Text = TotFlow1       ' Mix 1, total flow
            upDnFlow1.Value = TotFlow1 * -1

            upDnPctA2.Value = -1 * pct2a         ' Mix 2, gas A
            upDnPctB2.Value = -1 * pct2b         ' Mix 2, gas B
            upDnPctC2.Value = -1 * pct2c         ' Mix 2, gas C
            upDnPctD2.Value = -1 * pct2d         ' Mix 2, gas D
            tbxTFlow2.Text = TotFlow2       ' Mix 2, total flow
            upDnFlow2.Value = TotFlow2 * -1

            upDnPctA3.Value = -1 * pct3a         ' Mix 3, gas A
            upDnPctB3.Value = -1 * pct3b         ' Mix 3, gas B
            upDnPctC3.Value = -1 * pct3c         ' Mix 3, gas C
            upDnPctD3.Value = -1 * pct3d         ' Mix 3, gas D
            tbxTFlow3.Text = TotFlow3       ' Mix 3, total flow
            upDnFlow3.Value = TotFlow3 * -1

            upDnPctA4.Value = -1 * pct4a         ' Mix 4, gas A
            upDnPctB4.Value = -1 * pct4b         ' Mix 4, gas B
            upDnPctC4.Value = -1 * pct4c         ' Mix 4, gas C
            upDnPctD4.Value = -1 * pct4d         ' Mix 4, gas D
            tbxTFlow4.Text = TotFlow4       ' Mix 4, total flow
            upDnFlow4.Value = TotFlow4 * -1

            Dim result As String                            ' get filename and show in status strip at bottom of window
            result = System.IO.Path.GetFileName(fnConfig)   ' get plain filename without full path
            lblConfigStrip.Text = "Configuration file:  " & result
        End If
    End Sub



    '=========================================================================================================================================
    '                                    MENU SAVE CONFIGURATION FILE is selected
    '=========================================================================================================================================
    Private Sub SaveConfigurationToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveConfigurationToolStripMenuItem.Click
        On Error Resume Next

        ' collect all the values to be saved to the configuration file
        If btnFillGas0.Checked = True Then FillGas = 0
        If btnFillGas1.Checked = True Then FillGas = 1
        If btnFillGas2.Checked = True Then FillGas = 2
        If btnFillGas3.Checked = True Then FillGas = 3

        gas1 = ComboGas0.SelectedIndex
        gas2 = ComboGas1.SelectedIndex
        gas3 = ComboGas2.SelectedIndex
        gas4 = ComboGas3.SelectedIndex

        pct1a = upDnPctA1.Value * -1         ' Mix 1, gas A
        pct1b = upDnPctB1.Value * -1         ' Mix 1, gas B
        pct1c = upDnPctC1.Value * -1         ' Mix 1, gas C
        pct1d = upDnPctD1.Value * -1         ' Mix 1, gas D
        TotFlow1 = Val(tbxTFlow1.Text)  ' Mix 1, total flow

        pct2a = upDnPctA2.Value * -1         ' Mix 2, gas A
        pct2b = upDnPctB2.Value * -1         ' Mix 2, gas B
        pct2c = upDnPctC2.Value * -1         ' Mix 2, gas C
        pct2d = upDnPctD2.Value * -1         ' Mix 2, gas D
        TotFlow2 = Val(tbxTFlow2.Text)  ' Mix 2, total flow

        pct3a = upDnPctA3.Value * -1         ' Mix 3, gas A
        pct3b = upDnPctB3.Value * -1         ' Mix 3, gas B
        pct3c = upDnPctC3.Value * -1         ' Mix 3, gas C
        pct3d = upDnPctD3.Value * -1         ' Mix 3, gas D
        TotFlow3 = Val(tbxTFlow3.Text)  ' Mix 3, total flow

        pct4a = upDnPctA4.Value * -1         ' Mix 4, gas A
        pct4b = upDnPctB4.Value * -1         ' Mix 4, gas B
        pct4c = upDnPctC4.Value * -1         ' Mix 4, gas C
        pct4d = upDnPctD4.Value * -1         ' Mix 4, gas D
        TotFlow4 = Val(tbxTFlow4.Text)  ' Mix 4, total flow

        maxflow1 = Val(tbxMaxFlow1.Text)
        maxflow2 = Val(tbxMaxFlow2.Text)
        maxflow3 = Val(tbxMaxFlow3.Text)
        maxflow4 = Val(tbxMaxFlow4.Text)

        ' set up SaveFileDialogBox and Streamwriter
        sFile.FileName = "Save as..."
        sFile.Filter = "(*txt) | *.txt"
        sFile.OverwritePrompt = True
        sFile.CreatePrompt = True
        'sFile.CheckFileExists = True                ' this prevents creation of new file! Don't use it!
        sFile.ShowDialog()
        Dim W As New IO.StreamWriter(sFile.FileName)

        ' write the individual values 
        W.Write(gas1 & ",")
        W.Write(gas2 & ",")
        W.Write(gas3 & ",")
        W.Write(gas4 & ",")
        W.Write(pct1a & ",")
        W.Write(pct1b & ",")
        W.Write(pct1c & ",")
        W.Write(pct1d & ",")
        W.Write(TotFlow1 & ",")
        W.Write(pct2a & ",")
        W.Write(pct2b & ",")
        W.Write(pct2c & ",")
        W.Write(pct2d & ",")
        W.Write(TotFlow2 & ",")
        W.Write(pct3a & ",")
        W.Write(pct3b & ",")
        W.Write(pct3c & ",")
        W.Write(pct3d & ",")
        W.Write(TotFlow3 & ",")
        W.Write(pct4a & ",")
        W.Write(pct4b & ",")
        W.Write(pct4c & ",")
        W.Write(pct4d & ",")
        W.Write(TotFlow4 & ",")
        W.Write(maxflow1 & ",")
        W.Write(maxflow2 & ",")
        W.Write(maxflow3 & ",")
        W.Write(maxflow4 & ",")
        W.Write(FillGas)
        W.Close()

        Dim result As String                                    ' get filename and put on status bar at bottom of window
        result = System.IO.Path.GetFileName(sFile.FileName)     ' get plain filename without full path
        lblConfigStrip.Text = "Configuration file:  " & result
    End Sub



    '=========================================================================================================================================
    '                                    MENU LOAD SEQUENCER (EXPERIMENT) FILE is selected
    '=========================================================================================================================================
    Private Sub LoadTimedSequenceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadTimedSequenceToolStripMenuItem.Click
        Dim fnConfig As String
        On Error Resume Next

        Dim openFileDialog1 As New OpenFileDialog()
        openFileDialog1.Filter = "Exp Files (*.exp)|*.exp"
        openFileDialog1.Title = "Load Sequencer File"

        If openFileDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            fnConfig = openFileDialog1.FileName             ' selected file
            Dim reader As New IO.StreamReader(fnConfig, IO.FileMode.Open)

            'loop until end of file
            While reader.Peek <> -1                         ' checks for end-of-file
                Dim sFile As String = reader.ReadLine       ' reads all text as a single line

                Dim str() As String = sFile.Split(",")      ' splits text file using "," delimiter

                'get data from csv file into individual strings
                stepcombos(0).Text = str(0)
                stepboxes(0).Text = str(1)
                stepcombos(1).Text = str(2)
                stepboxes(1).Text = str(3)
                stepcombos(2).Text = str(4)
                stepboxes(2).Text = str(5)
                stepcombos(3).Text = str(6)
                stepboxes(3).Text = str(7)
                stepcombos(4).Text = str(8)
                stepboxes(4).Text = str(9)
                stepcombos(5).Text = str(10)
                stepboxes(5).Text = str(11)
                stepcombos(6).Text = str(12)
                stepboxes(6).Text = str(13)
                stepcombos(7).Text = str(14)
                stepboxes(7).Text = str(15)
                stepcombos(8).Text = str(16)
                stepboxes(8).Text = str(17)
                stepcombos(9).Text = str(18)
                stepboxes(9).Text = str(19)
                stepcombos(10).Text = str(20)
                stepboxes(10).Text = str(21)
                stepcombos(11).Text = str(22)
                stepboxes(11).Text = str(23)
                stepcombos(12).Text = str(24)
                stepboxes(12).Text = str(25)
                stepcombos(13).Text = str(26)
                stepboxes(13).Text = str(27)
                stepcombos(14).Text = str(28)
                stepboxes(14).Text = str(29)
            End While
            reader.Close()
            Dim result As String                            ' get filename and put on status strip at bottom of window
            result = System.IO.Path.GetFileName(fnConfig)   ' get plain filename without full path
            lblSequencerStrip.Text = "Sequencer file:  " & result
        End If
    End Sub



    '=========================================================================================================================================
    '                                    MENU SAVE SEQUENCER (EXPERIMENT) FILE is selected
    '=========================================================================================================================================
    Private Sub SaveTimedSequenceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveTimedSequenceToolStripMenuItem.Click
        On Error Resume Next
        ' set up SaveFileDialogBox and Streamwriter
        sFile.FileName = "Save as..."
        sFile.Filter = "(*exp) | *.exp"
        sFile.CreatePrompt = True
        'sFile.CheckFileExists = True   ' this prevents creation of new file! Don't use it!
        sFile.OverwritePrompt = True
        sFile.ShowDialog()
        Dim W As New IO.StreamWriter(sFile.FileName)

        W.Write(stepcombos(0).Text & ",")
        W.Write(stepboxes(0).Text & ",")
        W.Write(stepcombos(1).Text & ",")
        W.Write(stepboxes(1).Text & ",")
        W.Write(stepcombos(2).Text & ",")
        W.Write(stepboxes(2).Text & ",")
        W.Write(stepcombos(3).Text & ",")
        W.Write(stepboxes(3).Text & ",")
        W.Write(stepcombos(4).Text & ",")
        W.Write(stepboxes(4).Text & ",")
        W.Write(stepcombos(5).Text & ",")
        W.Write(stepboxes(5).Text & ",")
        W.Write(stepcombos(6).Text & ",")
        W.Write(stepboxes(6).Text & ",")
        W.Write(stepcombos(7).Text & ",")
        W.Write(stepboxes(7).Text & ",")
        W.Write(stepcombos(8).Text & ",")
        W.Write(stepboxes(8).Text & ",")
        W.Write(stepcombos(9).Text & ",")
        W.Write(stepboxes(9).Text & ",")
        W.Write(stepcombos(10).Text & ",")
        W.Write(stepboxes(10).Text & ",")
        W.Write(stepcombos(11).Text & ",")
        W.Write(stepboxes(11).Text & ",")
        W.Write(stepcombos(12).Text & ",")
        W.Write(stepboxes(12).Text & ",")
        W.Write(stepcombos(13).Text & ",")
        W.Write(stepboxes(13).Text & ",")
        W.Write(stepcombos(14).Text & ",")
        W.Write(stepboxes(14).Text)
        W.Close()

        Dim result As String
        result = System.IO.Path.GetFileName(sFile.FileName)   ' get plain filename without full path
        lblSequencerStrip.Text = "Sequencer file:  " & result
    End Sub




    '=========================================================================================================================================
    '                                               SEQUENCER ROUTINES
    '=========================================================================================================================================
    Private Sub btnHaltSequence_Click(sender As Object, e As EventArgs) Handles btnHaltSequence.Click
        duration = 0
        flgStop = True
        stepTimer.Stop()
        stepTimer.Enabled = False
        progTimer.Enabled = False
        ProgBar1.Value = 0

        For x = 0 To 14
            leds(x).BackColor = Color.RosyBrown
        Next
        seqRunning = False
    End Sub




    '=========================================================================================================================================
    '                                               TIMER 1 ROUTINES, EXECUTED EVERY 100mS
    '=========================================================================================================================================
    Public Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer.Tick
        On Error GoTo jumparound    ' exit if error on value entry

        '-----------------------------------------------------------------------------------------------------------------
        '           Make mix indicator red on running mixture, gray on ones not running
        '-----------------------------------------------------------------------------------------------------------------
        If mix_running = 1 Then
            ledRunning1.BackColor = Color.Red
        Else
            ledRunning1.BackColor = Color.RosyBrown    ' gray
        End If

        If mix_running = 2 Then
            ledRunning2.BackColor = Color.Red
        Else
            ledRunning2.BackColor = Color.RosyBrown    ' gray
        End If

        If mix_running = 3 Then
            ledRunning3.BackColor = Color.Red
        Else
            ledRunning3.BackColor = Color.RosyBrown    ' gray
        End If

        If mix_running = 4 Then
            ledRunning4.BackColor = Color.Red
        Else
            ledRunning4.BackColor = Color.RosyBrown    ' gray
        End If

        '-----------------------------------------------------------------------------------------------------------------
        '                                       Autofill gas values
        '-----------------------------------------------------------------------------------------------------------------
        ' If fill button is selected, use that gas as fill, otherwise user enters value directly
        ' Selected: textbox background color = blue, updn control is invisible
        ' Not selected: textbox background color = white, updn control is visible

        ' Gas 1 ****
        If btnFillGas0.Checked = True Then      ' gas 1 selected as fill gas
            ' Mix A1    ****
            upDnPctA1.Visible = False           ' make updn control invisible
            tbxPctA1.BackColor = Color.Aqua     ' change "fill" text box to blue
            If (Val(tbxPctB1.Text) + Val(tbxPctC1.Text) + Val(tbxPctD1.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctA1.Text = Format(100 - (Val(tbxPctB1.Text) + Val(tbxPctC1.Text) + Val(tbxPctD1.Text)), "##0.0") ' compute fill gas value
                upDnPctA1.Value = -1 * Val(tbxPctA1.Text) * 10  '*****************************
            Else
                tbxPctA1.Text = 0                                            ' other values too big, so make fill gas 0%
            End If

            ' Mix A2    ****
            upDnPctA2.Visible = False           ' make updn control invisible
            tbxPctA2.BackColor = Color.Aqua     ' change "fill" text box to blue
            If (Val(tbxPctB2.Text) + Val(tbxPctC2.Text) + Val(tbxPctD2.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctA2.Text = Format(100 - (Val(tbxPctB2.Text) + Val(tbxPctC2.Text) + Val(tbxPctD2.Text)), "##0.0") ' compute fill gas value
                upDnPctA2.Value = -1 * Val(tbxPctA2.Text) * 10  '*****************************
            Else
                tbxPctA2.Text = 0                                            ' other values too big, so make fill gas 0%
            End If

            ' Mix A3    ****
            upDnPctA3.Visible = False           ' make updn control invisible
            tbxPctA3.BackColor = Color.Aqua     ' change "fill" text box to blue
            If (Val(tbxPctB3.Text) + Val(tbxPctC3.Text) + Val(tbxPctD3.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctA3.Text = Format(100 - (Val(tbxPctB3.Text) + Val(tbxPctC3.Text) + Val(tbxPctD3.Text)), "##0.0") ' compute fill gas value
                upDnPctA3.Value = -1 * Val(tbxPctA3.Text) * 10  '*****************************
            Else
                tbxPctA3.Text = 0                                            ' other values too big, so make fill gas 0%
            End If

            ' Mix A4    ****
            upDnPctA4.Visible = False           ' make updn control invisible
            tbxPctA4.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctB4.Text) + Val(tbxPctC4.Text) + Val(tbxPctD4.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctA4.Text = Format(100 - (Val(tbxPctB4.Text) + Val(tbxPctC4.Text) + Val(tbxPctD4.Text)), "##0.0") ' compute fill gas value
                upDnPctA4.Value = -1 * Val(tbxPctA4.Text) * 10  '*****************************
            Else
                tbxPctA4.Text = 0                                            ' other values too big, so make fill gas 0%
            End If
        Else
            ' Mix A1
            upDnPctA1.Visible = True            ' make control visible
            tbxPctA1.BackColor = Color.White       ' make "fill" text box white
            If tbxPctA1.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctA1.Text = "" Then tbxPctA1.Text = 0   ' check for null value
                If Val(tbxPctA1.Text) > 100 Then tbxPctA1.Text = 100   ' limit value to 100%
                'If Val(tbxPctA1.Text) <> -1 * upDnPctA1.Value / 10 Then upDnPctA1.Value = -1 * Val(tbxPctA1.Text) * 10  '*****************************################
            End If

            ' Mix A2
            upDnPctA2.Visible = True            ' make control visible
            tbxPctA2.BackColor = Color.White       ' make "fill" text box white
            If tbxPctA2.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctA2.Text = "" Then tbxPctA2.Text = 0   ' check for null value
                If Val(tbxPctA2.Text) > 100 Then tbxPctA2.Text = 100   ' limit value to 100%
                'If tbxPctA2 <> upDnPctA2 / 10 Then upDnPctA2 = tbxPctA2 * 10
            End If

            ' Mix A3
            upDnPctA3.Visible = True            ' make control visible
            tbxPctA3.BackColor = Color.White       ' make "fill" text box white
            If tbxPctA3.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctA3.Text = "" Then tbxPctA3.Text = 0   ' check for null value
                If Val(tbxPctA3.Text) > 100 Then tbxPctA3.Text = 100   ' limit value to 100%
                'If tbxPctA3 <> upDnPctA3 / 10 Then upDnPctA3 = tbxPctA3 * 10
            End If

            ' Mix A4
            upDnPctA4.Visible = True            ' make control visible
            tbxPctA4.BackColor = Color.White       ' make "fill" text box white
            If tbxPctA4.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctA4.Text = "" Then tbxPctA4.Text = 0   ' check for null value
                If Val(tbxPctA4.Text) > 100 Then tbxPctA4.Text = 100   ' limit value to 100%
                'If tbxPctA4 <> upDnPctA4 / 10 Then upDnPctA4 = tbxPctA4 * 10
            End If
        End If

        '-----------------------------------------------------------------------------------------------------------------
        ' Gas 2 ****
        If btnFillGas1.Checked = True Then      ' gas 2 selected as fill gas
            ' Mix B1    ****
            upDnPctB1.Visible = False           ' make updn control invisible
            tbxPctB1.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA1.Text) + Val(tbxPctC1.Text) + Val(tbxPctD1.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctB1.Text = Format(100 - (Val(tbxPctA1.Text) + Val(tbxPctC1.Text) + Val(tbxPctD1.Text)), "##0.0") ' compute fill gas value
                upDnPctB1.Value = -1 * Val(tbxPctB1.Text) * 10  '*****************************
            Else
                tbxPctB1.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix B2    ****
            upDnPctB2.Visible = False           ' make updn control invisible
            tbxPctB2.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA2.Text) + Val(tbxPctC2.Text) + Val(tbxPctD2.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctB2.Text = Format(100 - (Val(tbxPctA2.Text) + Val(tbxPctC2.Text) + Val(tbxPctD2.Text)), "##0.0") ' compute fill gas value
                upDnPctB2.Value = -1 * Val(tbxPctB2.Text) * 10  '*****************************
            Else
                tbxPctB2.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix B3    ****
            upDnPctB3.Visible = False           ' make updn control invisible
            tbxPctB3.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA3.Text) + Val(tbxPctC3.Text) + Val(tbxPctD3.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctB3.Text = Format(100 - (Val(tbxPctA3.Text) + Val(tbxPctC3.Text) + Val(tbxPctD3.Text)), "##0.0") ' compute fill gas value
                upDnPctB3.Value = -1 * Val(tbxPctB3.Text) * 10  '*****************************
            Else
                tbxPctB3.Text = 0                     ' other values too big, so make fill gas 0%
            End If

            ' Mix B4    ****
            upDnPctB4.Visible = False           ' make updn control invisible
            tbxPctB4.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA4.Text) + Val(tbxPctC4.Text) + Val(tbxPctD4.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctB4.Text = Format(100 - (Val(tbxPctA4.Text) + Val(tbxPctC4.Text) + Val(tbxPctD4.Text)), "##0.0") ' compute fill gas value
                upDnPctB4.Value = -1 * Val(tbxPctB4.Text) * 10  '*****************************
            Else
                tbxPctB4.Text = 0                    ' other values too big, so make fill gas 0%
            End If
        Else
            ' Mix B1
            upDnPctB1.Visible = True            ' make control visible
            tbxPctB1.BackColor = Color.White       ' make "fill" text box white
            If tbxPctB1.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctB1.Text = "" Then tbxPctB1.Text = 0   ' check for null value
                If Val(tbxPctB1.Text) > 100 Then tbxPctB1.Text = 100   ' limit value to 100%
                'If tbxPctB1 <> upDnPctB1 / 10 Then upDnPctB1 = tbxPctB1 * 10
            End If

            ' Mix B2
            upDnPctB2.Visible = True            ' make control visible
            tbxPctB2.BackColor = Color.White       ' make "fill" text box white
            If tbxPctB2.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctB2.Text = "" Then tbxPctB2.Text = 0   ' check for null value
                If Val(tbxPctB2.Text) > 100 Then tbxPctB2.Text = 100   ' limit value to 100%
                'If tbxPctB2 <> upDnPctB2 / 10 Then upDnPctB2 = tbxPctB2 * 10
            End If

            ' Mix B3
            upDnPctB3.Visible = True            ' make control visible
            tbxPctB3.BackColor = Color.White       ' make "fill" text box white
            If tbxPctB3.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctB3.Text = "" Then tbxPctB3.Text = 0   ' check for null value
                If Val(tbxPctB3.Text) > 100 Then tbxPctB3.Text = 100   ' limit value to 100%
                'If tbxPctB3 <> upDnPctB3 / 10 Then upDnPctB3 = tbxPctB3 * 10
            End If

            ' Mix B4
            upDnPctB4.Visible = True            ' make control visible
            tbxPctB4.BackColor = Color.White       ' make "fill" text box white
            If tbxPctB4.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctB4.Text = "" Then tbxPctB4.Text = 0   ' check for null value
                If Val(tbxPctB4.Text) > 100 Then tbxPctB4.Text = 100   ' limit value to 100%
                'If tbxPctB4 <> upDnPctB4 / 10 Then upDnPctB4 = tbxPctB4 * 10
            End If
        End If

        '-----------------------------------------------------------------------------------------------------------------
        ' Gas 3 ****
        If btnFillGas2.Checked = True Then      ' gas 3 selected as fill gas
            ' Mix C1
            upDnPctC1.Visible = False           ' make updn control invisible
            tbxPctC1.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA1.Text) + Val(tbxPctB1.Text) + Val(tbxPctD1.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctC1.Text = Format(100 - (Val(tbxPctA1.Text) + Val(tbxPctB1.Text) + Val(tbxPctD1.Text)), "##0.0") ' compute fill gas value
                upDnPctC1.Value = -1 * Val(tbxPctC1.Text) * 10  '*****************************
            Else
                tbxPctC1.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix C2    ****
            upDnPctC2.Visible = False           ' make updn control invisible
            tbxPctC2.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA2.Text) + Val(tbxPctB2.Text) + Val(tbxPctD2.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctC2.Text = Format(100 - (Val(tbxPctA2.Text) + Val(tbxPctB2.Text) + Val(tbxPctD2.Text)), "##0.0") ' compute fill gas value
                upDnPctC2.Value = -1 * Val(tbxPctC2.Text) * 10  '*****************************
            Else
                tbxPctC2.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix C3    ****
            upDnPctC3.Visible = False           ' make updn control invisible
            tbxPctC3.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA3.Text) + Val(tbxPctB3.Text) + Val(tbxPctD3.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctC3.Text = Format(100 - (Val(tbxPctA3.Text) + Val(tbxPctB3.Text) + Val(tbxPctD3.Text)), "##0.0") ' compute fill gas value
                upDnPctC3.Value = -1 * Val(tbxPctC3.Text) * 10  '*****************************
            Else
                tbxPctC3.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix C4    ****
            upDnPctC4.Visible = False           ' make updn control invisible
            tbxPctC4.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA4.Text) + Val(tbxPctB4.Text) + Val(tbxPctD4.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctC4.Text = Format(100 - (Val(tbxPctA4.Text) + Val(tbxPctB4.Text) + Val(tbxPctD4.Text)), "##0.0") ' compute fill gas value
                upDnPctC4.Value = -1 * Val(tbxPctC4.Text) * 10  '*****************************
            Else
                tbxPctC4.Text = 0                    ' other values too big, so make fill gas 0%
            End If
        Else
            ' Mix C1
            upDnPctC1.Visible = True            ' make control visible
            tbxPctC1.BackColor = Color.White       ' make "fill" text box white
            If tbxPctC1.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctC1.Text = "" Then tbxPctC1.Text = 0   ' check for null value
                If Val(tbxPctC1.Text) > 100 Then tbxPctC1.Text = 100   ' limit value to 100%
                'If tbxPctC1 <> upDnPctC1 / 10 Then upDnPctC1 = tbxPctC1 * 10
            End If

            ' Mix C2
            upDnPctC2.Visible = True            ' make control visible
            tbxPctC2.BackColor = Color.White       ' make "fill" text box white
            If tbxPctC2.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctC2.Text = "" Then tbxPctC2.Text = 0   ' check for null value
                If Val(tbxPctC2.Text) > 100 Then tbxPctC2.Text = 100   ' limit value to 100%
                'If tbxPctC2 <> upDnPctC2 / 10 Then upDnPctC2 = tbxPctC2 * 10
            End If

            ' Mix C3
            upDnPctC3.Visible = True            ' make control visible
            tbxPctC3.BackColor = Color.White       ' make "fill" text box white
            If tbxPctC3.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctC3.Text = "" Then tbxPctC3.Text = 0   ' check for null value
                If Val(tbxPctC3.Text) > 100 Then tbxPctC3.Text = 100   ' limit value to 100%
                'If tbxPctC3 <> upDnPctC3 / 10 Then upDnPctC3 = tbxPctC3 * 10
            End If

            ' Mix C4
            upDnPctC4.Visible = True            ' make control visible
            tbxPctC4.BackColor = Color.White       ' make "fill" text box white
            If tbxPctC4.Text <> "." Then         ' don't act if entering value less than 1, .x
                If tbxPctC4.Text = "" Then tbxPctC4.Text = 0   ' check for null value
                If Val(tbxPctC4.Text) > 100 Then tbxPctC4.Text = 100   ' limit value to 100%
                'If tbxPctC4 <> upDnPctC4 / 10 Then upDnPctC4 = tbxPctC4 * 10
            End If
        End If

        '&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
        '-----------------------------------------------------------------------------------------------------------------
        ' Gas 4
        If btnFillGas3.Checked = True Then      ' gas 4 selected as fill gas
            ' Mix D1    ****
            upDnPctD1.Visible = False           ' make updn control invisible
            tbxPctD1.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA1.Text) + Val(tbxPctB1.Text) + Val(tbxPctC1.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctD1.Text = Format(100 - (Val(tbxPctA1.Text) + Val(tbxPctB1.Text) + Val(tbxPctC1.Text)), "##0.0") ' compute fill gas value
                upDnPctD1.Value = -1 * Val(tbxPctD1.Text) * 10  '*****************************
            Else
                tbxPctD1.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix D2    ****
            upDnPctD2.Visible = False           ' make updn control invisible
            tbxPctD2.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA2.Text) + Val(tbxPctB2.Text) + Val(tbxPctC2.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctD2.Text = Format(100 - (Val(tbxPctA2.Text) + Val(tbxPctB2.Text) + Val(tbxPctC2.Text)), "##0.0") ' compute fill gas value
                upDnPctD2.Value = -1 * Val(tbxPctD2.Text) * 10  '*****************************
            Else
                tbxPctD2.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix D3    ****
            upDnPctD3.Visible = False           ' make updn control invisible
            tbxPctD3.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA3.Text) + Val(tbxPctB3.Text) + Val(tbxPctC3.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctD3.Text = Format(100 - (Val(tbxPctA3.Text) + Val(tbxPctB3.Text) + Val(tbxPctC3.Text)), "##0.0") ' compute fill gas value
                upDnPctD3.Value = -1 * Val(tbxPctD3.Text) * 10  '*****************************
            Else
                tbxPctD3.Text = 0                    ' other values too big, so make fill gas 0%
            End If

            ' Mix D4
            upDnPctD4.Visible = False           ' make updn control invisible
            tbxPctD4.BackColor = Color.Aqua       ' change "fill" text box to blue
            If (Val(tbxPctA4.Text) + Val(tbxPctB4.Text) + Val(tbxPctC4.Text)) <= 100 Then    ' make sure total doesn't exceed 100%
                tbxPctD4.Text = Format(100 - (Val(tbxPctA4.Text) + Val(tbxPctB4.Text) + Val(tbxPctC4.Text)), "##0.0") ' compute fill gas value
                upDnPctD4.Value = -1 * Val(tbxPctD4.Text) * 10  '*****************************
            Else
                tbxPctD4.Text = 0                    ' other values too big, so make fill gas 0%
            End If
        Else
            ' Mix D1    ****
            upDnPctD1.Visible = True            ' make control visible
            tbxPctD1.BackColor = Color.White       ' make "fill" text box white
            If tbxPctD1.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctD1.Text = "" Then tbxPctD1.Text = 0   ' check for null value
                If Val(tbxPctD1.Text) > 100 Then tbxPctD1.Text = 100   ' limit value to 100%
                'If tbxPctC1 <> upDnPctC1 / 10 Then upDnPctC1 = tbxPctC1 * 10
            End If

            ' Mix D2    ****
            upDnPctD2.Visible = True            ' make control visible
            tbxPctD2.BackColor = Color.White       ' make "fill" text box white
            If tbxPctD2.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctD2.Text = "" Then tbxPctD2.Text = 0   ' check for null value
                If Val(tbxPctD2.Text) > 100 Then tbxPctD2.Text = 100   ' limit value to 100%
                'If tbxPctC2 <> upDnPctC2 / 10 Then upDnPctC2 = tbxPctC2 * 10
            End If

            ' Mix D3    ****
            upDnPctD3.Visible = True            ' make control visible
            tbxPctD3.BackColor = Color.White       ' make "fill" text box white
            If tbxPctD3.Text <> "." Then             ' don't act if entering value less than 1, .x
                If tbxPctD3.Text = "" Then tbxPctD3.Text = 0   ' check for null value
                If Val(tbxPctD3.Text) > 100 Then tbxPctD3.Text = 100   ' limit value to 100%
                'If tbxPctC3 <> upDnPctC3 / 10 Then upDnPctC3 = tbxPctC3 * 10
            End If

            ' Mix D4    ****
            upDnPctD4.Visible = True            ' make control visible
            tbxPctD4.BackColor = Color.White       ' make "fill" text box white
            If tbxPctD4.Text <> "." Then         ' don't act if entering value less than 1, .x
                If tbxPctD4.Text = "" Then tbxPctD4.Text = 0   ' check for null value
                If Val(tbxPctD4.Text) > 100 Then tbxPctD4.Text = 100   ' limit value to 100%
                'If tbxPctC4 <> upDnPctC4 / 10 Then upDnPctC4 = tbxPctC4 * 10
            End If
        End If
        '&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

        '-----------------------------------------------------------------------------------------------------------------
        ' Update total percent boxes by adding up individual gas percentages; should equal 100%!
        '  if greater than 100% (because of fill gas!) then change text to RED
        totalpct1 = Val(tbxPctA1.Text) + Val(tbxPctB1.Text) + Val(tbxPctC1.Text) + Val(tbxPctD1.Text)
        tbxTotPct1.Text = totalpct1 & "%"
        If totalpct1 > 100 Then
            tbxTotPct1.ForeColor = Color.Red
            flgFlowError16 = 1
        Else
            tbxTotPct1.ForeColor = Color.Black
            flgFlowError16 = 0
        End If

        totalpct2 = Val(tbxPctA2.Text) + Val(tbxPctB2.Text) + Val(tbxPctC2.Text) + Val(tbxPctD2.Text)
        tbxTotPct2.Text = totalpct2 & "%"
        If totalpct2 > 100 Then
            tbxTotPct2.ForeColor = Color.Red
            flgFlowError17 = 1
        Else
            tbxTotPct2.ForeColor = Color.Black
            flgFlowError17 = 0
        End If

        totalpct3 = Val(tbxPctA3.Text) + Val(tbxPctB3.Text) + Val(tbxPctC3.Text) + Val(tbxPctD3.Text)
        tbxTotPct3.Text = totalpct3 & "%"
        If totalpct3 > 100 Then
            tbxTotPct3.ForeColor = Color.Red
            flgFlowError18 = 1
        Else
            tbxTotPct3.ForeColor = Color.Black
            flgFlowError18 = 0
        End If

        totalpct4 = Val(tbxPctA4.Text) + Val(tbxPctB4.Text) + Val(tbxPctC4.Text) + Val(tbxPctD4.Text)
        tbxTotPct4.Text = totalpct4 & "%"
        If totalpct4 > 100 Then
            tbxTotPct4.ForeColor = Color.Red
            flgFlowError19 = 1
        Else
            tbxTotPct4.ForeColor = Color.Black
            flgFlowError19 = 0
        End If



        '-----------------------------------------------------------------------------------------------------------------
        ' Update individual mix flow text boxes from updn controls
        tbxPctA1.Text = (upDnPctA1.Value * -1) / 10
        tbxPctB1.Text = (upDnPctB1.Value * -1) / 10
        tbxPctC1.Text = (upDnPctC1.Value * -1) / 10
        tbxPctD1.Text = (upDnPctD1.Value * -1) / 10

        tbxPctA2.Text = (upDnPctA2.Value * -1) / 10
        tbxPctB2.Text = (upDnPctB2.Value * -1) / 10
        tbxPctC2.Text = (upDnPctC2.Value * -1) / 10
        tbxPctD2.Text = (upDnPctD2.Value * -1) / 10

        tbxPctA3.Text = (upDnPctA3.Value * -1) / 10
        tbxPctB3.Text = (upDnPctB3.Value * -1) / 10
        tbxPctC3.Text = (upDnPctC3.Value * -1) / 10
        tbxPctD3.Text = (upDnPctD3.Value * -1) / 10

        tbxPctA4.Text = (upDnPctA4.Value * -1) / 10
        tbxPctB4.Text = (upDnPctB4.Value * -1) / 10
        tbxPctC4.Text = (upDnPctC4.Value * -1) / 10
        tbxPctD4.Text = (upDnPctD4.Value * -1) / 10

        '-----------------------------------------------------------------------------------------------------------------
        '                                      Format display values
        '-----------------------------------------------------------------------------------------------------------------
        ' update individual flow displays for all 4 mixes
        ' color text red if outside range for that channel's flow controller
        ' MIX 1 ****

        tbxFlow1Mix1.Text = (Int((Val(tbxPctA1.Text) * Val(tbxTFlow1.Text)) / 100)) & " ml"
        If Val(tbxFlow1Mix1.Text) > 0 And (Val(tbxFlow1Mix1.Text) < (maxflow1 / 100) Or Val(tbxFlow1Mix1.Text) > maxflow1) Then ' display 0ml is ok
            tbxFlow1Mix1.ForeColor = Color.Red  ' red bold text to indicate error
            'tbxFlow1Mix1.Text.bold = True
            flgFlowError1 = 1
        Else
            tbxFlow1Mix1.ForeColor = Color.Black   ' normal black text
            'tbxFlow1Mix1.FontBold = False
            flgFlowError1 = 0
        End If

        tbxFlow2Mix1.Text = (Int((Val(tbxPctB1.Text) * Val(tbxTFlow1.Text)) / 100)) & " ml"
        If Val(tbxFlow2Mix1.Text) > 0 And (Val(tbxFlow2Mix1.Text) < (maxflow2 / 100) Or Val(tbxFlow2Mix1.Text) > maxflow2) Then
            tbxFlow2Mix1.ForeColor = Color.Red
            'tbxFlow2Mix1.FontBold = True
            flgFlowError2 = 1
        Else
            tbxFlow2Mix1.ForeColor = Color.Black
            'tbxFlow2Mix1.FontBold = False
            flgFlowError2 = 0
        End If

        tbxFlow3Mix1.Text = (Int((Val(tbxPctC1.Text) * Val(tbxTFlow1.Text)) / 100)) & " ml"
        If Val(tbxFlow3Mix1.Text) > 0 And (Val(tbxFlow3Mix1.Text) < (maxflow3 / 100) Or Val(tbxFlow3Mix1.Text) > maxflow3) Then
            tbxFlow3Mix1.ForeColor = Color.Red
            'tbxFlow3Mix1.FontBold = True
            flgFlowError3 = 1
        Else
            tbxFlow3Mix1.ForeColor = Color.Black
            'tbxFlow3Mix1.FontBold = False
            flgFlowError3 = 0
        End If

        tbxFlow4Mix1.Text = (Int((Val(tbxPctD1.Text) * Val(tbxTFlow1.Text)) / 100)) & " ml"
        If Val(tbxFlow4Mix1.Text) > 0 And (Val(tbxFlow4Mix1.Text) < (maxflow4 / 100) Or Val(tbxFlow4Mix1.Text) > maxflow4) Then
            tbxFlow4Mix1.ForeColor = Color.Red
            'tbxFlow3Mix1.FontBold = True
            flgFlowError4 = 1
        Else
            tbxFlow4Mix1.ForeColor = Color.Black
            'tbxFlow3Mix1.FontBold = False
            flgFlowError4 = 0
        End If
        '-----------------------------------------------------------------------------------------------------------------
        ' MIX 2 ****
        tbxFlow1Mix2.Text = (Int((Val(tbxPctA2.Text) * Val(tbxTFlow2.Text)) / 100)) & " ml"
        If Val(tbxFlow1Mix2.Text) > 0 And (Val(tbxFlow1Mix2.Text) < (maxflow1 / 100) Or Val(tbxFlow1Mix2.Text) > maxflow1) Then
            tbxFlow1Mix2.ForeColor = Color.Red
            'tbxFlow1Mix2.FontBold = True
            flgFlowError4 = 1
        Else
            tbxFlow1Mix2.ForeColor = Color.Black
            'tbxFlow1Mix2.FontBold = False
            flgFlowError4 = 0
        End If

        tbxFlow2Mix2.Text = (Int((Val(tbxPctB2.Text) * Val(tbxTFlow2.Text)) / 100)) & " ml"
        If Val(tbxFlow2Mix2.Text) > 0 And (Val(tbxFlow2Mix2.Text) < (maxflow2 / 100) Or Val(tbxFlow2Mix2.Text) > maxflow2) Then
            tbxFlow2Mix2.ForeColor = Color.Red
            'tbxFlow2Mix2.FontBold = True
            flgFlowError5 = 1
        Else
            tbxFlow2Mix2.ForeColor = Color.Black
            'tbxFlow2Mix2.FontBold = False
            flgFlowError5 = 0
        End If

        tbxFlow3Mix2.Text = (Int((Val(tbxPctC2.Text) * Val(tbxTFlow2.Text)) / 100)) & " ml"
        If Val(tbxFlow3Mix2.Text) > 0 And (Val(tbxFlow3Mix2.Text) < (maxflow3 / 100) Or Val(tbxFlow3Mix2.Text) > maxflow3) Then
            tbxFlow3Mix2.ForeColor = Color.Red
            'tbxFlow3Mix2.FontBold = True
            flgFlowError6 = 1
        Else
            tbxFlow3Mix2.ForeColor = Color.Black
            'tbxFlow3Mix2.FontBold = False
            flgFlowError6 = 0
        End If

        tbxFlow4Mix2.Text = (Int((Val(tbxPctD2.Text) * Val(tbxTFlow2.Text)) / 100)) & " ml"   '###############################################
        If Val(tbxFlow4Mix2.Text) > 0 And (Val(tbxFlow4Mix2.Text) < (maxflow4 / 100) Or Val(tbxFlow4Mix2.Text) > maxflow4) Then
            tbxFlow4Mix2.ForeColor = Color.Red
            'tbxFlow3Mix2.FontBold = True
            flgFlowError6 = 1
        Else
            tbxFlow4Mix2.ForeColor = Color.Black
            'tbxFlow3Mix2.FontBold = False
            flgFlowError7 = 0
        End If

        '-----------------------------------------------------------------------------------------------------------------
        ' MIX 3 ****
        tbxFlow1Mix3.Text = (Int((Val(tbxPctA3.Text) * Val(tbxTFlow3.Text)) / 100)) & " ml"
        If Val(tbxFlow1Mix3.Text) > 0 And (Val(tbxFlow1Mix3.Text) < (maxflow1 / 100) Or Val(tbxFlow1Mix3.Text) > maxflow1) Then
            tbxFlow1Mix3.ForeColor = Color.Red
            'tbxFlow1Mix3.FontBold = True
            flgFlowError7 = 1
        Else
            tbxFlow1Mix3.ForeColor = Color.Black
            'tbxFlow1Mix3.FontBold = False
            flgFlowError7 = 0
        End If

        tbxFlow2Mix3.Text = (Int((Val(tbxPctB3.Text) * Val(tbxTFlow3.Text)) / 100)) & " ml"
        If Val(tbxFlow2Mix3.Text) > 0 And (Val(tbxFlow2Mix3.Text) < (maxflow2 / 100) Or Val(tbxFlow2Mix3.Text) > maxflow2) Then
            tbxFlow2Mix3.ForeColor = Color.Red
            'tbxFlow2Mix3.FontBold = True
            flgFlowError8 = 1
        Else
            tbxFlow2Mix3.ForeColor = Color.Black
            'tbxFlow2Mix3.FontBold = False
            flgFlowError8 = 0
        End If

        tbxFlow3Mix3.Text = (Int((Val(tbxPctC3.Text) * Val(tbxTFlow3.Text)) / 100)) & " ml"
        If Val(tbxFlow3Mix3.Text) > 0 And (Val(tbxFlow3Mix3.Text) < (maxflow3 / 100) Or Val(tbxFlow3Mix3.Text) > maxflow3) Then
            tbxFlow3Mix3.ForeColor = Color.Red
            'tbxFlow3Mix3.FontBold = True
            flgFlowError9 = 1
        Else
            tbxFlow3Mix3.ForeColor = Color.Black
            'tbxFlow3Mix3.FontBold = False
            flgFlowError9 = 0
        End If

        tbxFlow4Mix3.Text = (Int((Val(tbxPctD3.Text) * Val(tbxTFlow3.Text)) / 100)) & " ml"  '#############################################
        If Val(tbxFlow4Mix3.Text) > 0 And (Val(tbxFlow4Mix3.Text) < (maxflow4 / 100) Or Val(tbxFlow4Mix3.Text) > maxflow4) Then
            tbxFlow4Mix3.ForeColor = Color.Red
            'tbxFlow3Mix3.FontBold = True
            flgFlowError10 = 1
        Else
            tbxFlow4Mix3.ForeColor = Color.Black
            'tbxFlow3Mix3.FontBold = False
            flgFlowError10 = 0
        End If

        '-----------------------------------------------------------------------------------------------------------------
        ' MIX 4 ****
        tbxFlow1Mix4.Text = (Int((Val(tbxPctA4.Text) * Val(tbxTFlow4.Text)) / 100)) & " ml"
        If Val(tbxFlow1Mix4.Text) > 0 And (Val(tbxFlow1Mix4.Text) < (maxflow1 / 100) Or Val(tbxFlow1Mix4.Text) > maxflow1) Then
            tbxFlow1Mix4.ForeColor = Color.Red
            'tbxFlow1Mix4.FontBold = True
            flgFlowError10 = 1
        Else
            tbxFlow1Mix4.ForeColor = Color.Black
            'tbxFlow1Mix4.FontBold = False
            flgFlowError10 = 0
        End If

        tbxFlow2Mix4.Text = (Int((Val(tbxPctB4.Text) * Val(tbxTFlow4.Text)) / 100)) & " ml"
        If Val(tbxFlow2Mix4.Text) > 0 And (Val(tbxFlow2Mix4.Text) < (maxflow2 / 100) Or Val(tbxFlow2Mix4.Text) > maxflow2) Then
            tbxFlow2Mix4.ForeColor = Color.Red
            'tbxFlow2Mix4.FontBold = True
            flgFlowError11 = 1
        Else
            tbxFlow2Mix4.ForeColor = Color.Black
            'tbxFlow2Mix4.FontBold = False
            flgFlowError11 = 0
        End If

        tbxFlow3Mix4.Text = (Int((Val(tbxPctC4.Text) * Val(tbxTFlow4.Text)) / 100)) & " ml"
        If Val(tbxFlow3Mix4.Text) > 0 And (Val(tbxFlow3Mix4.Text) < (maxflow3 / 100) Or Val(tbxFlow3Mix4.Text) > maxflow3) Then
            tbxFlow3Mix4.ForeColor = Color.Red
            'tbxFlow3Mix4.FontBold = True
            flgFlowError12 = 1
        Else
            tbxFlow3Mix4.ForeColor = Color.Black
            'tbxFlow3Mix4.FontBold = False
            flgFlowError12 = 0
        End If

        tbxFlow4Mix4.Text = (Int((Val(tbxPctD4.Text) * Val(tbxTFlow4.Text)) / 100)) & " ml"
        If Val(tbxFlow4Mix4.Text) > 0 And (Val(tbxFlow4Mix4.Text) < (maxflow4 / 100) Or Val(tbxFlow4Mix4.Text) > maxflow4) Then
            tbxFlow4Mix4.ForeColor = Color.Red
            'tbxFlow3Mix4.FontBold = True
            flgFlowError13 = 1
        Else
            tbxFlow4Mix4.ForeColor = Color.Black
            'tbxFlow3Mix4.FontBold = False
            flgFlowError13 = 0
        End If

        Call tbxErrMsg_TextChanged(sender, e)

jumparound:     ' vector here on error during value entry

    End Sub

End Class

