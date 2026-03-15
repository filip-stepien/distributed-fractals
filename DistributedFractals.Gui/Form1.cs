using DistributedFractals.Core;

namespace DistributedFractals.Gui;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        var generator = new MandelbrotGenerator();
        var bitmap = generator.Generate(ClientSize.Width, ClientSize.Height);

        var pictureBox = new PictureBox
        {
            Image = bitmap,
            Dock = DockStyle.Fill
        };

        Controls.Add(pictureBox);
    }
}
