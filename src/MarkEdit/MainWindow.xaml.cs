using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Diagnostics;
namespace MarkEdit;

public partial class MainWindow : Window
{
    private string lastCompiledPdfPath = null;
    public MainWindow()
    {
        InitializeComponent();
        latexEditor.Text = @"\documentclass{article}
\begin{document}
Hello, MarkEdit!
\end{document}";

        //_ = CompileAsync();

    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async Task CompileAsync()
    {
        try
        {
            string latexCode = latexEditor.Text;

            string tempDir = Path.Combine(Path.GetTempPath(), "MarkEditTemp");
            Directory.CreateDirectory(tempDir);

            string baseName = "document";
            string texFile = Path.Combine(tempDir, baseName + ".tex");
            string pdfFile = Path.Combine(tempDir, baseName + ".pdf");

            // Delete old PDF so we always get fresh output
            if (File.Exists(pdfFile))
            {
                File.Delete(pdfFile);
            }

            File.WriteAllText(texFile, latexCode);

            var p = new Process();
            p.StartInfo.FileName = "pdflatex";
            p.StartInfo.Arguments = $"-interaction=nonstopmode -output-directory \"{tempDir}\" \"{texFile}\"";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            string output = await p.StandardOutput.ReadToEndAsync();
            string error = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();

            if (File.Exists(pdfFile))
            {
                lastCompiledPdfPath = pdfFile;

                await pdfViewer.EnsureCoreWebView2Async();
                // Force refresh by adding dummy query param
                pdfViewer.Source = new Uri(pdfFile + "?v=" + DateTime.Now.Ticks);
            }
            else
            {
                MessageBox.Show("PDF not generated. Compiler output:\n" + output + "\n" + error,
                    "Compile Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error during compile:\n" + ex.Message,
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }




    private async void Compile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string latexCode = latexEditor.Text;

            string tempDir = Path.Combine(Path.GetTempPath(), "MarkEditTemp");
            Directory.CreateDirectory(tempDir);

            string texFile = Path.Combine(tempDir, "document.tex");
            File.WriteAllText(texFile, latexCode);

            Process p = new();
            p.StartInfo.FileName = "pdflatex";
            p.StartInfo.Arguments = $"-interaction=nonstopmode -output-directory \"{tempDir}\" \"{texFile}\"";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();
            p.WaitForExit();

            string pdfFile = Path.Combine(tempDir, "document.pdf");


            if (File.Exists(pdfFile))
            {
                lastCompiledPdfPath = pdfFile;
                //pdfViewer.Navigate(new Uri(pdfFile));
                await pdfViewer.EnsureCoreWebView2Async();
                pdfViewer.Source = new Uri(pdfFile);

            }
            else
            {
                MessageBox.Show("PDF not generated. Compiler output:\n" + output + "\n" + error,
                    "Compile Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error during compile:\n" + ex.Message,
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Download_Click(object sender, RoutedEventArgs e)
    {

        if (lastCompiledPdfPath == null || !File.Exists(lastCompiledPdfPath))
        {
            MessageBox.Show("Please compile first before downloading.",
                "No PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveFileDialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = "document.pdf"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                File.Copy(lastCompiledPdfPath, saveFileDialog.FileName, overwrite: true);
                MessageBox.Show("PDF exported successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save PDF:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void Compile_Click2(object sender, RoutedEventArgs e)
    {
        await CompileAsync();
    }


    private void Resizer_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        double newLeftWidth = Column0.ActualWidth + e.HorizontalChange;
        double newRightWidth = Column2.ActualWidth - e.HorizontalChange;

        if (newLeftWidth > 200 && newRightWidth > 200)
        {
            Column0.Width = new GridLength(newLeftWidth, GridUnitType.Star);
            Column2.Width = new GridLength(newRightWidth, GridUnitType.Star);
        }
    }
}