import glob, os

out = open("Icons.xaml", "w")
out.write('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"\n    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n')

for file in glob.glob("Icons/*.png"):
    name = os.path.splitext(os.path.basename(file))[0]
    out.write('    <BitmapImage x:Key="fugue-{0}" UriSource="Icons/{0}.png" />\n'.format(name))

out.write('</ResourceDictionary>')

out2 = open("Images.xaml", "w")
out2.write('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"\n    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n')

out2.write('    <Style x:Key="IconStyle" TargetType="{x:Type Image}">\n')
out2.write('        <Setter Property="Width" Value="16" />\n')
out2.write('        <Setter Property="Height" Value="16" />\n')
out2.write('        <Style.Triggers>\n')
out2.write('            <Trigger Property="IsEnabled" Value="False">\n')
out2.write('                <Setter Property="Opacity" Value="0.15" />\n')
out2.write('            </Trigger>\n')
out2.write('        </Style.Triggers>\n')
out2.write('    </Style>\n')

for file in glob.glob("Icons/*.png"):
    name = os.path.splitext(os.path.basename(file))[0]
    out2.write('    <Image x:Key="image-fugue-{0}" x:Shared="False" Style="{{StaticResource IconStyle}}">\n'.format(name))
    out2.write('        <Image.Source>\n')
    out2.write('            <BitmapImage UriSource="Icons/{0}.png" />\n'.format(name))
    out2.write('        </Image.Source>\n')
    out2.write('    </Image>\n')

out2.write('</ResourceDictionary>')

out3 = open("FugueIcons.cs", "w")
out3.write('namespace Fugue\n')
out3.write('{\n')
out3.write('    public class Icons\n')
out3.write('    {\n')
for file in glob.glob("Icons/*.png"):
    name = os.path.splitext(os.path.basename(file))[0]
    out3.write('        public const string {0} = "fugue-{1}";\n'.format(name.title().replace("-", ""), name))
out3.write('    }\n')
out3.write('}\n')
