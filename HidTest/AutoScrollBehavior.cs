
//public class AutoScrollBehavior : Behavior<TextBox>
//{
//    protected override void OnAttached()
//    {
//        base.OnAttached();
//        AssociatedObject.TextChanged += TextBox_TextChanged;
//    }

//    protected override void OnDetaching()
//    {
//        AssociatedObject.TextChanged -= TextBox_TextChanged;
//        base.OnDetaching();
//    }

//    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
//    {
//        var textBox = (TextBox)sender;
//        textBox.ScrollToEnd();
//    }
//}
