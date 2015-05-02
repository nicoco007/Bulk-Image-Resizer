Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Drawing.Imaging
Imports System.Reflection
Imports System.Threading
Imports System.Windows.Forms.ListView
Imports System.Text

Public Class Form1
    Private WithEvents FolderBrowserDialog As New FolderSelectDialog

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim InterpolationModes As New ArrayList

        InterpolationModes.Add(New TaggedComboBoxItem("Bicubic", InterpolationMode.Bicubic))
        InterpolationModes.Add(New TaggedComboBoxItem("Bilinear", InterpolationMode.Bilinear))
        InterpolationModes.Add(New TaggedComboBoxItem("Nearest Neighbor", InterpolationMode.NearestNeighbor))

        ComboBox1.DataSource = InterpolationModes
        ComboBox1.DisplayMember = "Text"
        ComboBox1.ValueMember = "Tag"

        ComboBox1.SelectedIndex = 0

        Dim ImageFormats As New ArrayList
        ImageFormats.Add(New TaggedComboBoxItem("PNG", ImageFormat.Png))
        ImageFormats.Add(New TaggedComboBoxItem("JPG", ImageFormat.Jpeg))
        ImageFormats.Add(New TaggedComboBoxItem("BMP", ImageFormat.Bmp))

        ComboBox2.DataSource = ImageFormats
        ComboBox2.DisplayMember = "Text"
        ComboBox2.ValueMember = "Tag"

        ComboBox2.SelectedIndex = 0

        OutputDirectoryTextBox.Text = My.Computer.FileSystem.SpecialDirectories.MyPictures & "\Bulk Image Resizer\" & DateTime.Now.ToString("yyyy-MM-dd")
    End Sub

    Public Shared Function ResizeImage(Image As Image, Size As Size, InterpolationMode As InterpolationMode) As Image
        Dim NewWidth As Integer = Size.Width
        Dim NewHeight As Integer = Size.Height

        Dim NewImage As Bitmap = New Bitmap(NewWidth, NewHeight)

        Using GH As Graphics = Graphics.FromImage(NewImage)
            GH.InterpolationMode = InterpolationMode
            GH.DrawImage(Image, 0, 0, NewWidth + 1, NewHeight + 1)
        End Using

        Return NewImage
    End Function

    Private Sub OpenFileDialog_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog.FileOk
        Dim list As New List(Of String)
        For Each Item As ListViewItem In ListView1.Items
            list.Add(Item.Tag)
        Next

        Dim sb As New StringBuilder

        For Each FileName In OpenFileDialog.FileNames
            If Not list.Contains(FileName) Then
                Dim File As FileInfo = New FileInfo(FileName)
                Dim Item As New ListViewItem(File.Name)
                Item.Name = Path.GetFileNameWithoutExtension(File.Name)
                Item.SubItems.Add(Path.GetDirectoryName(File.FullName))
                Dim Image As Image = Image.FromFile(File.FullName)
                Item.SubItems.Add(Image.Size.Width & "×" & Image.Size.Height)
                Item.Tag = File.FullName
                ListView1.Items.Add(Item)
            Else
                sb.AppendLine(String.Format("Skipped item '{0}', already in list.", FileName))
            End If
        Next

        If Not sb.ToString = "" Then
            MessageBox.Show(sb.ToString)
        End If

        Label5.Text = String.Format("{0} items", ListView1.Items.Count)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OpenFileDialog.ShowDialog()
        Button5.Enabled = True
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim list As New List(Of String)
        For Each Item As ListViewItem In ListView1.Items
            list.Add(Item.Tag)
        Next

        Dim sb As New StringBuilder

        If FolderBrowserDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
            Dim Directory As New DirectoryInfo(FolderBrowserDialog.SelectedFolder)
            For Each File In Directory.GetFiles
                If File.Extension.ToLower = ".png" Or File.Extension.ToLower = ".jpg" Or File.Extension.ToLower = ".jpeg" Or File.Extension.ToLower = ".bmp" Then
                    If Not list.Contains(File.FullName) Then
                        Dim Item As New ListViewItem(File.Name)
                        Item.SubItems.Add(Path.GetDirectoryName(File.FullName))
                        Dim Image As Image = Image.FromFile(File.FullName)
                        Item.SubItems.Add(Image.Size.Width & "×" & Image.Size.Height)
                        ListView1.Items.Add(Item)
                    Else
                        sb.AppendLine(String.Format("Skipped item '{0}', already in list.", File.Name))
                    End If
                End If
            Next

            If Not sb.ToString = "" Then
                MessageBox.Show(sb.ToString)
            End If
        End If

        Button5.Enabled = True
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        For Each Item As ListViewItem In ListView1.SelectedItems
            Item.Remove()
        Next

        Button5.Enabled = False
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        ListView1.Items.Clear()
    End Sub

    Private Items As New List(Of ListViewItem)
    Private ImageFormat As ImageFormat
    Private InterpolationMode As InterpolationMode
    Private Prefix, Suffix, ReplaceFrom, ReplaceTo, OutputFolder As String

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Items.Clear()
        For Each Item As ListViewItem In ListView1.Items
            Items.Add(Item)
        Next

        Prefix = IIf(PrefixCheckBox.Checked, PrefixTextBox.Text, "")
        Suffix = IIf(SuffixCheckBox.Checked, SuffixTextBox.Text, "")

        ReplaceFrom = IIf(ReplaceCheckBox.Checked, ReplaceFromTextBox.Text, "")
        ReplaceTo = IIf(ReplaceCheckBox.Checked, ReplaceToTextBox.Text, "")

        ProgressBar1.Maximum = ListView1.Items.Count()

        InterpolationMode = ComboBox1.SelectedItem.Tag
        ImageFormat = ComboBox2.SelectedItem.Tag

        OutputFolder = OutputDirectoryTextBox.Text
        If Not My.Computer.FileSystem.DirectoryExists(OutputFolder) Then
            My.Computer.FileSystem.CreateDirectory(OutputFolder)
        End If

        BackgroundWorker.RunWorkerAsync()
    End Sub

    Private Sub BackgroundWorker_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker.DoWork
        Dim ItemNumber As Integer = 0
        For Each Item As ListViewItem In Items
            Dim Original As Image = Image.FromFile(Item.Tag)
            Dim Resized As Image

            If (RadioButton1.Checked) Then
                Resized = ResizeImage(Original, New Size(NumericUpDown1.Value, NumericUpDown2.Value), InterpolationMode)
            Else
                Resized = ResizeImage(Original, New Size(NumericUpDown1.Value / 100 * Original.Width, NumericUpDown2.Value / 100 * Original.Height), InterpolationMode)
            End If

            Dim Stream As New MemoryStream
            If String.IsNullOrEmpty(ReplaceFrom) Then
                Resized.Save(OutputFolder & "\" & Prefix & Item.Name & Suffix & "." & ImageFormat.ToString.ToLower, ImageFormat)
            Else
                Resized.Save(OutputFolder & "\" & Prefix & Item.Name.Replace(ReplaceFrom, ReplaceTo) & Suffix & "." & ImageFormat.ToString.ToLower, ImageFormat)
            End If

            ItemNumber += 1
            ProgressBar_Progress(ItemNumber)
        Next
    End Sub

    Private Sub ProgressBar_Progress(Progress As Integer)
        If ProgressBar1.InvokeRequired Then
            ProgressBar1.Invoke(Sub() ProgressBar_Progress(Progress))
        Else
            ProgressBar1.Value = Progress
        End If
    End Sub

    Private Sub PrefixCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles PrefixCheckBox.CheckedChanged
        PrefixTextBox.Enabled = PrefixCheckBox.Checked
    End Sub

    Private Sub SuffixCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles SuffixCheckBox.CheckedChanged
        SuffixTextBox.Enabled = SuffixCheckBox.Checked
    End Sub

    Private Sub ReplaceCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles ReplaceCheckBox.CheckedChanged
        ReplaceToTextBox.Enabled = ReplaceCheckBox.Checked
        ReplaceFromTextBox.Enabled = ReplaceCheckBox.Checked
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If FolderBrowserDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
            OutputDirectoryTextBox.Text = FolderBrowserDialog.SelectedFolder
        End If
    End Sub
End Class

Public Class TaggedComboBoxItem
    Private _Text As String
    Private _Tag

    Public Property Text() As String
        Get
            Return _Text
        End Get
        Set(value As String)
            _Text = value
        End Set
    End Property

    Public Property Tag()
        Get
            Return _Tag
        End Get
        Set(value)
            _Tag = value
        End Set
    End Property

    Public Sub New(Text As String, Tag As Object)
        Me._Text = Text
        Me._Tag = Tag
    End Sub
End Class