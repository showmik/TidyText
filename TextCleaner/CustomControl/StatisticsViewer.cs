using System.Windows;
using System.Windows.Controls;

namespace TidyText.CustomControl
{
    public class StatisticsViewer : Control
    {
        private Label label;
        private Label content;

        public static DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(StatisticsViewer), new PropertyMetadata("", OnLabelPropertyChanged));
        public static DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(string), typeof(StatisticsViewer), new PropertyMetadata("", OnLabelPropertyChanged));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Content
        {
            get => (string)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        private static void OnLabelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (StatisticsViewer)d;
            viewer.UpdateLabelContent();
        }

        static StatisticsViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatisticsViewer), new FrameworkPropertyMetadata(typeof(StatisticsViewer)));
        }

        public override void OnApplyTemplate()
        {
            label = Template.FindName("SV_Label", this) as Label;
            content = Template.FindName("SV_Content", this) as Label;

            UpdateLabelContent();

            base.OnApplyTemplate();
        }

        private void UpdateLabelContent()
        {
            if (label != null)
            {
                label.Content = Label;
            }

            if (content != null)
            {
                content.Content = Content;
            }
        }
    }
}
