using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.Data;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer disTimer = new DispatcherTimer();

        private TickBar mistake_TickBar;
        private TickBar goodStuff_TickBar;

        private Sessions sessionsRoot = new Sessions();
        private List<Session> theSessions = new List<Session>();
        private List<VideoObject> videoObjects = new List<VideoObject>();
        private List<double> mistakeTicksValue = new List<double>();
        private List<double> goodStuffTicksValue = new List<double>();
        private List<double> theAllTicksValue = new List<double>();
        private List<string> sentences = new List<string>();

        Dictionary<int, string> mistakeAnnotation = new Dictionary<int, string>();
        Dictionary<int, string> goodStuffAnnotation = new Dictionary<int, string>();
        Dictionary<int, string> theAllAnnotation = new Dictionary<int, string>();
        Dictionary<int, string> mistakeFeedback = new Dictionary<int, string>();
        Dictionary<int, string> goodStuffFeedback = new Dictionary<int, string>();

        Dictionary<double, List<string>> mistakeCheckBox = new Dictionary<double, List<string>>();
        Dictionary<double, List<string>> mistakeCheckBox1 = new Dictionary<double, List<string>>();
        Dictionary<double, List<string>> goodStuffCheckBox = new Dictionary<double, List<string>>();
        Dictionary<double, List<string>> goodStuffCheckBox1 = new Dictionary<double, List<string>>();

        List<MistakeCheckBox> mistakeCheckBoxList = new List<MistakeCheckBox>();
        List<GoodStuffCheckBox> goodStuffCheckBoxList = new List<GoodStuffCheckBox>();

        string feedbackVideoId = string.Empty;
        List<string> mp4Files = new List<string>();

        string feedbackOrdnerPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            // First get the current execution directory
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Back to Video directory
            string theVideoPath = System.IO.Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "Video");

            // Get the full path to the Video directory
            string theVideoFullPath = System.IO.Path.GetFullPath(theVideoPath);

            // Get all mp4 files in the Video directory
            try
            {
                mp4Files = Directory.GetFiles(theVideoFullPath, "*.mp4", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }

            string theFirstVideoPath = mp4Files[0];

            //mediaElement.Source = new Uri("C:\\Users\\Chufan Zhang\\Desktop\\Bachelorarbeit\\Bachelor_Code\\Video\\520374c.mp4");
            //string initialVideoId = "520374c";
            mediaElement.Source = new Uri(theFirstVideoPath);
            string theFirstVideoID = System.IO.Path.GetFileNameWithoutExtension(theFirstVideoPath);
            string theFirstVideoID1 = theFirstVideoID.Substring(0, theFirstVideoID.Length - 1);
            string initialVideoId = theFirstVideoID1;
            feedbackVideoId = initialVideoId;

            // Get the path of the Json directory
            string jsonPath = System.IO.Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "Json");
            string jsonFilePath = System.IO.Path.GetFullPath(jsonPath);

            // Reading data from Json file
            //sessionsRoot = Read_JsonData("C:\\Users\\Chufan Zhang\\Desktop\\Bachelorarbeit\\Bachelor_Code\\Json\\PracticeSession.json");
            sessionsRoot = Read_JsonData(jsonFilePath + "\\PracticeSession.json");
            theSessions = sessionsRoot.sessions;

            string videoOrdnerP = System.IO.Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "FeedBack");
            feedbackOrdnerPath = System.IO.Path.GetFullPath(videoOrdnerP);
            /*
            string v1 = theSessions[0].start;
            string v2 = theSessions[1].start;
            DateTime dateTimeV1 = DateTime.Parse(v1);
            DateTime dateTimeV2 = DateTime.Parse(v2);

            string videoTitel1 = dateTimeV1.ToString("yyyy-MM-dd HH:mm:ss");
            string videoTitel2 = dateTimeV2.ToString("yyyy-MM-dd HH:mm:ss");
            
            videoObjects.Add(new VideoObject { Titel = videoTitel1, VideoPath = "C:\\Users\\Chufan Zhang\\Desktop\\Bachelorarbeit\\Bachelor_Code\\Video\\520374c.mp4"});
            videoObjects.Add(new VideoObject { Titel = videoTitel2, VideoPath = "C:\\Users\\Chufan Zhang\\Desktop\\Bachelorarbeit\\Bachelor_Code\\Video\\5344641c.mp4"});
            VideoListBox.ItemsSource = videoObjects;
            */

            // Set VideoListBox, Titel is the start time, Path is the video path
            foreach (Session s in theSessions)
            {
                string sID = s.videoId;
                string v = s.start;
                DateTime d = DateTime.Parse(v);
                string videoTitel = d.ToString("yyyy-MM-dd HH:mm:ss");
                foreach (string file in mp4Files)
                {
                    string m = System.IO.Path.GetFileNameWithoutExtension(file);
                    string m1 = m.Substring(0, m.Length - 1);
                    if (sID == m1)
                    {
                        // Add Video Objects
                        videoObjects.Add(new VideoObject { Titel = videoTitel, VideoPath = file });
                    }

                }
            }
            VideoListBox.ItemsSource = videoObjects;

            GetTicksValues(theSessions, initialVideoId);
            SetSentencesScrollViewer(theSessions, initialVideoId);
            SetMistakeScrollViewer(theSessions, initialVideoId);
            SetGoodStuffScrollViewer(theSessions, initialVideoId);
            SetFeedBackTextBox(theSessions, initialVideoId);
            //SetFeedBackTextBox(theSessions, initialVideoId);

            mediaElement.MediaOpened += MediaElement_MediaOpened;
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            videoSlider.Minimum = 0;
            videoSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            //Debug.WriteLine("videoSlider.Mximum：" + videoSlider.Maximum);

            UpdateTickBar();

            disTimer = new DispatcherTimer();
            // Setting the time interval
            disTimer.Interval = TimeSpan.FromMilliseconds(30);
            disTimer.Tick += new EventHandler(time_tick);

            disTimer.Start();
        }

        private void time_tick(object sender, EventArgs e)
        {
            disTimer.Stop();

            // Update the slider value only when there is no mouse dragging the slider
            if (videoSlider.IsMouseCaptureWithin != true)
            {
                videoSlider.Value = mediaElement.Position.TotalMilliseconds;
            }

            disTimer.Start();

            //Debug.WriteLine("activated");
        }

        // Update Tickbar
        private void UpdateTickBar()
        {
            mistake_TickBar = videoSlider.Template.FindName("Mistake_TickBar", videoSlider) as TickBar;

            if (mistake_TickBar != null)
            {
                // Set the width of the mistake_TickBar to the width of the Slider
                // keeping it visually aligned so that the scale of the TickBar matches the position of the Slider
                mistake_TickBar.Width = videoSlider.ActualWidth;
                // The position of the mistake_TickBar is set to the bottom of the Slider
                mistake_TickBar.Placement = TickBarPlacement.Bottom;
            }

            goodStuff_TickBar = videoSlider.Template.FindName("GoodStuff_TickBar", videoSlider) as TickBar;

            if (goodStuff_TickBar != null)
            {
                goodStuff_TickBar.Width = videoSlider.ActualWidth;
                goodStuff_TickBar.Placement = TickBarPlacement.Top;
            }
        }

        // Read Json file
        private Sessions Read_JsonData(string path)
        {
            // Read the Json file according to the give path
            string jsonString = File.ReadAllText(path);

            // Deserialize to Sessions object
            Sessions theRoot = JsonSerializer.Deserialize<Sessions>(jsonString);

            return theRoot;
        }

        // Play the video
        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Play();
        }

        // Pause the video
        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
        }

        // Selects the Titel of the video in the ListBox, plays the corresponding video, and updates the related data
        private void VideoListBox_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (VideoListBox.SelectedItem != null)
            {
                mediaElement.Stop();
                disTimer.Stop();

                mistakeTicksValue.Clear();
                goodStuffTicksValue.Clear();
                theAllTicksValue.Clear();
                sentences.Clear();

                mistakeAnnotation.Clear();
                goodStuffAnnotation.Clear();
                theAllAnnotation.Clear();

                mistakeCheckBox.Clear();
                goodStuffCheckBox.Clear();

                mistakeCheckBoxList.Clear();
                goodStuffCheckBoxList.Clear();

                mistakeFeedback.Clear();
                goodStuffFeedback.Clear();

                // Get the selected video object
                var selectedVideo = VideoListBox.SelectedItem as VideoObject;

                mediaElement.Source = new Uri(selectedVideo.VideoPath);

                string newVideoPath = selectedVideo.VideoPath;
                string newVideoName = System.IO.Path.GetFileNameWithoutExtension(newVideoPath);
                string newVideoTitel = newVideoName.Substring(0, newVideoName.Length - 1);

                feedbackVideoId = newVideoTitel;

                GetTicksValues(theSessions, newVideoTitel);
                SetSentencesScrollViewer(theSessions, newVideoTitel);
                SetMistakeScrollViewer(theSessions, newVideoTitel);
                SetGoodStuffScrollViewer(theSessions, newVideoTitel);
                SetFeedBackTextBox(theSessions, newVideoTitel);

                // Reset the color of the scale
                mistake_TickBar.Fill = Brushes.Red;
                goodStuff_TickBar.Fill = Brushes.Blue;
                mistake_TickBar = videoSlider.Template.FindName("Mistake_TickBar", videoSlider) as TickBar;
                goodStuff_TickBar = videoSlider.Template.FindName("GoodStuff_TickBar", videoSlider) as TickBar;
                mistake_TickBar.Ticks = new DoubleCollection(mistakeTicksValue);
                goodStuff_TickBar.Ticks = new DoubleCollection(goodStuffTicksValue);

                mediaElement.MediaOpened += MediaElement_MediaOpened;


            }
        }

        // Get the ticks values and related data
        private void GetTicksValues(List<Session> sessionList, string theVideoID)
        {

            mistakeTicksValue.Clear();
            goodStuffTicksValue.Clear();
            mistakeAnnotation.Clear();
            goodStuffAnnotation.Clear();

            // Find the corresponding session in the sessionList based on the given video ID
            int sessionID = 0;
            for (int x = 0; x < sessionList.Count; x++)
            {
                if (sessionList[x].videoId == theVideoID)
                {
                    sessionID = x;
                    break;
                }
            }

            // Get the actions of a session
            List<Action> action = sessionList[sessionID].actions;

            // Iterate through the list of actions to get the time and corresponding information for mistake and good stuff
            foreach (Action a in action)
            {
                string actionStart = a.start;
                int actionStartTime = Math.Abs((int)TimeSpan.Parse(actionStart).TotalMilliseconds);
                string theLogAction = a.logAction;

                if (a.mistake == true)
                {
                    if (mistakeAnnotation.ContainsKey(actionStartTime))
                    {
                        mistakeAnnotation[actionStartTime] = mistakeAnnotation[actionStartTime] + "," + theLogAction;

                        mistakeCheckBox[actionStartTime].Add(theLogAction);
                    }
                    else
                    {
                        mistakeAnnotation.Add(actionStartTime, theLogAction);

                        mistakeCheckBox.Add(actionStartTime, new List<string> { theLogAction });
                    }
                }
                else
                {
                    if (goodStuffAnnotation.ContainsKey(actionStartTime))
                    {
                        goodStuffAnnotation[actionStartTime] = goodStuffAnnotation[actionStartTime] + "," + theLogAction;

                        goodStuffCheckBox[actionStartTime].Add(theLogAction);
                    }
                    else
                    {
                        goodStuffAnnotation.Add(actionStartTime, theLogAction);

                        goodStuffCheckBox.Add(actionStartTime, new List<string> { theLogAction });
                    }
                }


            }
            foreach (var v in mistakeAnnotation)
            {
                theAllAnnotation.Add(v.Key, v.Value);
            }

            foreach (var v in goodStuffAnnotation)
            {
                theAllAnnotation.Add(v.Key, v.Value);
            }

            // save the scale value of the mistake
            foreach (var v in mistakeAnnotation)
            {
                mistakeTicksValue.Add(v.Key);
            }

            foreach (var v in goodStuffAnnotation)
            {
                goodStuffTicksValue.Add(v.Key);
            }

            theAllTicksValue = mistakeTicksValue.Concat(goodStuffTicksValue).ToList();

            /*
            foreach (double d in theAllTicksValue)
            {
                Debug.WriteLine(d);
            }
            */

        }

        // Set the ScrollViewer for Sentences
        private void SetSentencesScrollViewer(List<Session> sessionList, string theVideoID)
        {
            // Find the corresponding session in the sessionList based on the given video ID
            int sessionID = 0;
            for (int x = 0; x < sessionList.Count; x++)
            {
                if (sessionList[x].videoId == theVideoID)
                {
                    sessionID = x;
                    break;
                }
            }

            List<Sentence> theSentences = sessionList[sessionID].sentences;

            foreach (Sentence sen in theSentences)
            {
                bool identified = sen.wasIdentified;
                string s = sen.sentence;

                if (identified == true)
                {
                    sentences.Add(s);
                }

            }

            // Display sentences in sentences on new lines
            foreach (string sentence in sentences)
            {
                TextBlock textBlock = new TextBlock { Text = sentence, TextWrapping = TextWrapping.Wrap };
                SentencesStackPanek.Children.Add(textBlock);
            }


        }

        // Set the ScrollViewer of the mistake
        private void SetMistakeScrollViewer(List<Session> sessionList, string theVideoID)
        {
            MistakeStackPanel.Children.Clear();
            mistakeCheckBoxList.Clear();

            // Find the corresponding session in the sessionList based on the given video ID
            int sessionID = 0;
            for (int x = 0; x < sessionList.Count; x++)
            {
                if (sessionList[x].videoId == theVideoID)
                {
                    sessionID = x;
                    break;
                }
            }

            List<Action> action = sessionList[sessionID].actions;
            List<string> mistakeLogaction = new List<string>();

            //Debug.WriteLine(action.Count);
            // Iterate through the list of actions to get the contents of the mistake and avoid duplicate storage
            foreach (Action a in action)
            {
                string logAct = a.logAction;
                if (a.mistake == true)
                {
                    if (!mistakeLogaction.Contains(logAct))
                    {
                        mistakeLogaction.Add(logAct);
                    }
                }

            }

            //Debug.WriteLine(mistakeLogaction.Count);

            // create a checkbox for each mistake
            foreach (string s in mistakeLogaction)
            {
                // The checkbox's checked state is initialized to true, bacause in the initial state all scales are displayed on the slider
                MistakeCheckBox mistakeCheckbox = new MistakeCheckBox { Mistake = s, Checked = true };

                // save the state of all mistakeCheckboxes
                mistakeCheckBoxList.Add(mistakeCheckbox);

                CheckBox checkbox = new CheckBox();
                // Scaling is displayed in the textblock
                TextBlock textBlock = new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap };
                checkbox.SetBinding(CheckBox.IsCheckedProperty, new Binding("Checked") { Source = mistakeCheckbox });
                checkbox.Checked += CheckBox_Checked;
                checkbox.Unchecked += CheckBox_UnChecked;

                // By adding StackPanel, the checkbox and textblock can be displayed on one line
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;

                stackPanel.Children.Add(checkbox);
                stackPanel.Children.Add(textBlock);

                MistakeStackPanel.Children.Add(stackPanel);

            }

        }

        // Set the ScrollViewer of good stuff
        private void SetGoodStuffScrollViewer(List<Session> sessionList, string theVideoID)
        {
            GoodStuffStackPanel.Children.Clear();
            goodStuffCheckBoxList.Clear();

            // Find the cprresponding session in the sessionList based on the given video ID
            int sessionID = 0;
            for (int x = 0; x < sessionList.Count; x++)
            {
                if (sessionList[x].videoId == theVideoID)
                {
                    sessionID = x;
                    break;
                }
            }

            List<Action> action = sessionList[sessionID].actions;
            List<string> goodStuffLogaction = new List<string>();

            //Debug.WriteLine(action.Count);
            // Iterate through the list of actions to get the contents of goodStuff and avoid duplicate storage
            foreach (Action a in action)
            {
                string logAct = a.logAction;
                if (a.mistake != true)
                {
                    if (!goodStuffLogaction.Contains(logAct))
                    {
                        goodStuffLogaction.Add(logAct);
                    }
                }

            }

            // Create a checkBox for each goodStuff
            foreach (string s in goodStuffLogaction)
            {
                GoodStuffCheckBox goodStuffCheckbox = new GoodStuffCheckBox { GoodStuff = s, Checked = true };
                goodStuffCheckBoxList.Add(goodStuffCheckbox);

                CheckBox checkBox = new CheckBox();
                TextBlock textBlock = new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap };

                checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("Checked") { Source = goodStuffCheckbox });
                checkBox.Checked += CheckBox_Checked;
                checkBox.Unchecked += CheckBox_UnChecked;

                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;
                stackPanel.Children.Add(checkBox);
                stackPanel.Children.Add(textBlock);

                GoodStuffStackPanel.Children.Add(stackPanel);
            }
        }

        // This method is called when the CheckBox is checked, update the data related to mistake and goodStuff
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("checkBox is checked");
            mistakeCheckBox1.Clear();
            goodStuffCheckBox1.Clear();

            List<string> checkBoxM = new List<string>();

            // Determine which mistakes in the mistakeCheckBoxList are checked, and save the checked mistakes in the checkBox
            foreach (MistakeCheckBox m in mistakeCheckBoxList)
            {
                if (m.Checked == true)
                {
                    checkBoxM.Add(m.Mistake);
                }
            }

            // Iterate over the checked mistake, then iterate over the mistakeCheckBox, determine whether the checked mistake
            // is in the key and value of the mistakeCheckBox, if so, save this key and value in the mistakeCheckBox1
            foreach (string s in checkBoxM)
            {
                foreach (var v in mistakeCheckBox)
                {
                    if (v.Value.Contains(s))
                    {
                        if (mistakeCheckBox1.ContainsKey(v.Key))
                        {
                            mistakeCheckBox1[v.Key].Add(s);

                        }
                        else
                        {
                            mistakeCheckBox1.Add(v.Key, new List<string> { s });
                        }
                    }
                }
            }

            /*
            foreach (var v in mistakeCheckBox1)
            {
                Debug.WriteLine(v.Key);
                
                foreach (var vv in v.Value)
                {
                    Debug.WriteLine("mistakes：" + vv);
                }
            }
            */

            List<string> checkBoxG = new List<string>();

            foreach (GoodStuffCheckBox g in goodStuffCheckBoxList)
            {
                if (g.Checked == true)
                {
                    checkBoxG.Add(g.GoodStuff);
                }
            }

            foreach (string s in checkBoxG)
            {
                foreach (var v in goodStuffCheckBox)
                {
                    if (v.Value.Contains(s))
                    {
                        if (goodStuffCheckBox1.ContainsKey(v.Key))
                        {
                            goodStuffCheckBox1[v.Key].Add(s);

                        }
                        else
                        {
                            goodStuffCheckBox1.Add(v.Key, new List<string> { s });
                        }
                    }
                }
            }

            /*
            foreach (var v in goodStuffCheckBox1)
            {
                Debug.WriteLine(v.Key);

                foreach (var vv in v.Value) 
                {
                    Debug.WriteLine("good stuff：" + vv);
                }
            }
            */

        }

        // This method is called when the CheckBox is unchecked, update the data related to mistake and goodStuff
        private void CheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("checkBox is unchecked");
            mistakeCheckBox1.Clear();
            goodStuffCheckBox1.Clear();

            List<string> checkBoxM = new List<string>();

            foreach (MistakeCheckBox m in mistakeCheckBoxList)
            {
                if (m.Checked == true)
                {
                    checkBoxM.Add(m.Mistake);
                }
            }

            foreach (string s in checkBoxM)
            {
                foreach (var v in mistakeCheckBox)
                {
                    if (v.Value.Contains(s))
                    {
                        if (mistakeCheckBox1.ContainsKey(v.Key))
                        {
                            mistakeCheckBox1[v.Key].Add(s);

                        }
                        else
                        {
                            mistakeCheckBox1.Add(v.Key, new List<string> { s });
                        }
                    }
                }
            }

            /*
            foreach (var v in mistakeCheckBox1)
            {
                Debug.WriteLine(v.Key);
                
                foreach (var vv in v.Value)
                {
                    Debug.WriteLine("mistakes：" + vv);
                }
            }
            */


            List<string> checkBoxG = new List<string>();

            foreach (GoodStuffCheckBox g in goodStuffCheckBoxList)
            {
                if (g.Checked == true)
                {
                    checkBoxG.Add(g.GoodStuff);
                }
            }

            foreach (string s in checkBoxG)
            {
                foreach (var v in goodStuffCheckBox)
                {
                    if (v.Value.Contains(s))
                    {
                        if (goodStuffCheckBox1.ContainsKey(v.Key))
                        {
                            goodStuffCheckBox1[v.Key].Add(s);

                        }
                        else
                        {
                            goodStuffCheckBox1.Add(v.Key, new List<string> { s });
                        }
                    }
                }
            }

            /*
            foreach (var v in goodStuffCheckBox1)
            {
                Debug.WriteLine(v.Key);

                foreach (var vv in v.Value)
                {
                    Debug.WriteLine("good stuff：" + vv);
                }
            }
            */
        }

        // Implement slider drag
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            int sliderValue = (int)videoSlider.Value;

            TimeSpan ts = new TimeSpan(0, 0, 0, 0, sliderValue);
            mediaElement.Position = ts;

            //Debug.WriteLine("valuechanged is triggered ");
        }

        // Initialize the Slider scale
        private void Slider_TickLoaded(object sender, RoutedEventArgs e)
        {

            mistake_TickBar = videoSlider.Template.FindName("Mistake_TickBar", videoSlider) as TickBar;
            goodStuff_TickBar = videoSlider.Template.FindName("GoodStuff_TickBar", videoSlider) as TickBar;

            mistake_TickBar.Ticks = new DoubleCollection(mistakeTicksValue);
            goodStuff_TickBar.Ticks = new DoubleCollection(goodStuffTicksValue);

            //UpdateTickBar();

        }

        // Implement clickling on the scale of the slider to jump and automatically fast-forward one second
        private void PART_Thumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int SliderValue = (int)videoSlider.Value;
            TimeSpan ts = mediaElement.Position;
            TimeSpan ts1 = ts.Subtract(TimeSpan.FromSeconds(1));
            mediaElement.Position = ts1;

        }

        // Implement a hover alert to display the scale's corresponding information when the mouse hovers over the scale 
        private void videoSlider_MouseEnter(object sender, MouseEventArgs e)
        {
            double tickIndex = GetMousePosition(e.GetPosition(videoSlider).X);
            //Debug.WriteLine("mouse position：" + e.GetPosition(videoSlider).X);
            string popupText = theAllAnnotation[(int)tickIndex];

            ShowPopup(popupText);

        }

        // Hide hover tips on mouseover
        private void videoSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            HidePopup();
        }

        // Get the mouse hover position and return the nearest scale
        private double GetMousePosition(double mouse)
        {
            // Get the width of the slider, the actual width, not the set width
            double sliderWidth = videoSlider.ActualWidth;

            // The slider is in units of time, so divide by the length of the video to calculate the width of each scale
            double tickWidth = sliderWidth / (videoSlider.Maximum + 1);

            // Calculate the scale corresponding hover position,
            // divide the mouse position by the scale width to get the corresponding scale position
            double tickIndex = Math.Round(mouse / tickWidth);

            double closestTick = -1;
            double minDistance = double.MaxValue;

            // Find the nearest scale by looping

            foreach (double d in theAllTicksValue)
            {
                double distance = Math.Abs(tickIndex - d);
                if (distance < minDistance)
                {
                    closestTick = d;
                    minDistance = distance;
                }
            }
            //Debug.WriteLine(closestTick);

            return closestTick;
        }

        // Show Hover Tips
        private void ShowPopup(string text)
        {
            Popup_TextBlock.Text = text;
            popup.IsOpen = true;
        }

        // Hide Hover Tips
        private void HidePopup()
        {
            popup.IsOpen = false;
        }

        // Implement the click event of the filter button
        // Based on the checked state of the checkbox, update the scale of the slider
        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("Click on the Fliter button");

            mistakeTicksValue.Clear();
            goodStuffTicksValue.Clear();
            theAllAnnotation.Clear();

            theAllTicksValue.Clear();

            // mistakeCheckBox1 save the checked mistakes, update the scale of the slider according to mistakeCheckBox1 
            foreach (var v in mistakeCheckBox1)
            {
                mistakeTicksValue.Add(v.Key);
                string s = string.Join(",", v.Value);
                theAllAnnotation.Add((int)v.Key, s);

                // Update theAllTicksValue, for determining the most recent scale on mouse hover
                theAllTicksValue.Add(v.Key);
            }

            foreach (var v in goodStuffCheckBox1)
            {
                goodStuffTicksValue.Add(v.Key);
                string s = string.Join(",", v.Value);
                theAllAnnotation.Add((int)v.Key, s);

                theAllTicksValue.Add(v.Key);
            }

            mistake_TickBar = videoSlider.Template.FindName("Mistake_TickBar", videoSlider) as TickBar;
            goodStuff_TickBar = videoSlider.Template.FindName("GoodStuff_TickBar", videoSlider) as TickBar;

            // Determine whether a mistake is checked, if so, update the scale of the mistake_TickBar,
            // if not then null, scale color set to transparent
            if (mistakeTicksValue.Count > 0)
            {
                mistake_TickBar.Fill = Brushes.Red;
                mistake_TickBar.Ticks = new DoubleCollection(mistakeTicksValue);
            }
            else
            {
                mistake_TickBar.Fill = Brushes.Transparent;
                mistake_TickBar.Ticks = null;
            }

            if (goodStuffTicksValue.Count > 0)
            {
                goodStuff_TickBar.Fill = Brushes.Blue;
                goodStuff_TickBar.Ticks = new DoubleCollection(goodStuffTicksValue);
            }
            else
            {
                goodStuff_TickBar.Fill = Brushes.Transparent;
                goodStuff_TickBar.Ticks = null;
            }

            UpdateTickBar();

        }

        // Set the content of the Feedback
        private void SetFeedBackTextBox(List<Session> sessionList, string theVideoID)
        {
            TextBox_Feedback.Text = string.Empty;
            Dictionary<string, int> goodStuffCumulativeTime = new Dictionary<string, int>();
            Dictionary<string, int> mistakeCumulativeTime = new Dictionary<string, int>();

            // Find the corresponding session based on the video ID
            int sessionID = 0;
            for (int x = 0; x < sessionList.Count; x++)
            {
                if (sessionList[x].videoId == theVideoID)
                {
                    sessionID = x;
                    break;
                }
            }

            // Calculate session duration
            string preStart = sessionList[sessionID].start;
            string preEnd = sessionList[sessionID].end;
            DateTime dtT = DateTime.Parse(preStart);
            DateTime dtE = DateTime.Parse(preEnd);
            string preStartTime = dtT.ToString("HH:mm:ss.fffffff");
            string preEndTime = dtE.ToString("HH:mm:ss.fffffff");
            TimeSpan preT = TimeSpan.Parse(preStartTime);
            TimeSpan preE = TimeSpan.Parse(preEndTime);
            TimeSpan preTimeSpan = preE - preT;
            int preTime = (int)preTimeSpan.TotalMilliseconds;
            //int preStartTime1 = (int)TimeSpan.Parse(preStartTime).TotalMilliseconds;
            //int preEndTime1 = (int)TimeSpan.Parse(preEndTime).TotalMilliseconds;
            //int preTime = preEndTime1 - preStartTime1;
            //Debug.WriteLine("preTime:" + preTime);

            List<Action> action = sessionList[sessionID].actions;
            List<string> theWorstMistakeList = new List<string>();
            List<string> mistakeInFeedback = new List<string>();
            List<string> goodStuffInFeedback = new List<string>();

            foreach (Action a in action)
            {
                // Calculate the duration of each action
                string actionStart = a.start;
                int actionStartTime = Math.Abs((int)TimeSpan.Parse(actionStart).TotalMilliseconds);
                string actionEnd = a.end;
                int actionEndTime = Math.Abs((int)TimeSpan.Parse(actionEnd).TotalMilliseconds);
                string theLogAction = a.logAction;
                int time = actionEndTime - actionStartTime;
                //Debug.WriteLine("time:" + time);

                // Save mistakes that persist throughout the video
                if (a.mistake == true)
                {
                    if (time >= preTime)
                    {

                        if (!theWorstMistakeList.Contains(theLogAction))
                        {
                            theWorstMistakeList.Add(theLogAction);
                        }
                    }
                }

                // Save mistake and good stuff in mistakeFeedback and goodStuffFeedback, respectively
                if (a.mistake == true)
                {

                    // If the time is not in mistakeFeedback, add a new key-value pair, and if it exsists,
                    // merge the mistake into a string
                    if (!mistakeFeedback.ContainsKey(time))
                    {
                        mistakeFeedback.Add(time, theLogAction);
                    }
                    else
                    {
                        mistakeFeedback[time] += "," + theLogAction;
                    }

                    if (!mistakeInFeedback.Contains(theLogAction))
                    {
                        mistakeInFeedback.Add(theLogAction);
                    }


                    if (!mistakeCumulativeTime.ContainsKey(theLogAction))
                    {
                        mistakeCumulativeTime.Add(theLogAction, time);
                    }
                    else
                    {
                        mistakeCumulativeTime[theLogAction] += time;
                    }

                }
                else
                {

                    if (!goodStuffFeedback.ContainsKey(time))
                    {
                        goodStuffFeedback.Add(time, theLogAction);
                    }
                    else
                    {
                        goodStuffFeedback[time] += "," + theLogAction;
                    }

                    if (!goodStuffInFeedback.Contains(theLogAction))
                    {
                        goodStuffInFeedback.Add(theLogAction);
                    }

                    if (!goodStuffCumulativeTime.ContainsKey(theLogAction))
                    {
                        goodStuffCumulativeTime.Add(theLogAction, time);
                    }
                    else
                    {
                        goodStuffCumulativeTime[theLogAction] += time;
                    }
                }
            }

            List<int> mistakeKey = new List<int>();
            List<string> mistakeValue = new List<string>();

            foreach (var v in mistakeFeedback)
            {
                mistakeKey.Add(v.Key);
            }

            // Ascending order based on time
            mistakeKey.Sort();

            // Save the mistake in the mistakeFeedback in mistakeValue according to the order of the mistakeKey
            foreach (int i in mistakeKey)
            {
                foreach (var v in mistakeFeedback)
                {
                    if (i == v.Key)
                    {
                        mistakeValue.Add(v.Value);
                    }
                }
            }

            int mistakeCount = mistakeValue.Count;
            string otherMistake = string.Empty;
            List<string> langMistakeInFeedback = new List<string>();
            List<String> langMistakeInFeedback1 = new List<String>();

            // Find the mistake that lasts the longest
            string langM = mistakeValue[mistakeCount - 1];
            langMistakeInFeedback = langM.Split(',').ToList();
            langMistakeInFeedback1 = langMistakeInFeedback.Distinct().ToList();
            // Save mistakes other than the one that lasts the longest
            foreach (string s in mistakeInFeedback)
            {
                if (!langMistakeInFeedback1.Contains(s))
                {
                    otherMistake = otherMistake + "," + s;
                }
            }

            List<int> goodStuffKey = new List<int>();
            List<string> goodStuffvalue = new List<string>();

            foreach (var v in goodStuffFeedback)
            {
                goodStuffKey.Add(v.Key);
            }

            goodStuffKey.Sort();

            foreach (int g in goodStuffKey)
            {
                foreach (var v in goodStuffFeedback)
                {
                    if (g == v.Key)
                    {
                        goodStuffvalue.Add(v.Value);
                    }
                }
            }

            int goodStuffCount = goodStuffvalue.Count;
            string otherGoodStuff = string.Empty;
            List<string> langGoodStuffInFeedback = new List<string>();
            List<string> langGoodStuffInFeedback1 = new List<string>();
            string langG = goodStuffvalue[goodStuffCount - 1];
            langGoodStuffInFeedback = langG.Split(',').ToList();
            langGoodStuffInFeedback1 = langGoodStuffInFeedback.Distinct().ToList();
            foreach (string s in goodStuffInFeedback)
            {
                if (!langGoodStuffInFeedback1.Contains(s))
                {
                    otherGoodStuff = otherGoodStuff + "," + s;
                }
            }

            // Stroe the error that lasts the whole video in the string theWorstMistake
            string theWorstMistake = string.Empty;
            foreach (string s in theWorstMistakeList)
            {
                theWorstMistake = s + "," + theWorstMistake;
            }

            // Save mistake and goodstuff, and the corresponding evaluation ratings
            Dictionary<string, string> mistakeGrade = new Dictionary<string, string>();
            Dictionary<string, string> goodStuffGrade = new Dictionary<string, string>();

            foreach (var v in goodStuffCumulativeTime)
            {
                // Calculate the duration as a percentage of the total length of the video
                double goodStuffPerecentage = Math.Round((double)v.Value / preTime * 100, 2);
                string grade = string.Empty;
                if (goodStuffPerecentage > 93.33)
                {
                    grade = "A";
                }
                else if (goodStuffPerecentage > 73.33 && goodStuffPerecentage < 93.33)
                {
                    grade = "B";
                }
                else if (goodStuffPerecentage > 53.33 && goodStuffPerecentage < 73.33)
                {
                    grade = "C";
                }
                else if (goodStuffPerecentage > 33.33 && goodStuffPerecentage < 53.33)
                {
                    grade = "D";
                }
                else
                {
                    grade = "F";
                }

                goodStuffGrade.Add(v.Key, grade);
            }

            foreach (var v in mistakeCumulativeTime)
            {
                double mistakePerecentage = Math.Round((double)v.Value / preTime * 100, 2);
                string grade = string.Empty;
                if (mistakePerecentage < 33.33)
                {
                    grade = "A";
                }
                else if (mistakePerecentage > 33.33 && mistakePerecentage < 53.33)
                {
                    grade = "B";
                }
                else if (mistakePerecentage > 53.33 && mistakePerecentage < 73.33)
                {
                    grade = "C";
                }
                else if (mistakePerecentage > 73.33 && mistakePerecentage < 93.33)
                {
                    grade = "D";
                }
                else
                {
                    grade = "F";
                }

                mistakeGrade.Add(v.Key, grade);
            }

            string theLangMsitake = string.Join(",", langMistakeInFeedback1);
            string theLangGoodStuff = string.Join(",", langGoodStuffInFeedback1);
            string mf = string.Empty;
            foreach (var v in mistakeGrade)
            {
                mf = mf + v.Key + " is graded " + v.Value + ".";
            }
            string gs = string.Empty;
            foreach (var v in goodStuffGrade)
            {
                gs = gs + v.Key + " is graded " + v.Value + ".";
            }

            // Content of Feedback
            TextBox_Feedback.Text += "The most important mistakes to watch out for is " + theLangMsitake;
            TextBox_Feedback.Text += "\r\n\r\nOther mistakes to watch our are " + otherMistake;
            TextBox_Feedback.Text += "\r\n\r\nThe best good action is " + theLangGoodStuff;
            TextBox_Feedback.Text += "\r\n\r\nOther good actions are " + otherGoodStuff;
            //TextBox_Feedback.Text += "\r\n\r\nThe following are the mistakes made in this presentation exercise:";

            // If there is a mistake that lasts the whole video, it will be shown in the feedback
            if (theWorstMistake != string.Empty)
            {
                TextBox_Feedback.Text += "\r\n\r\nThese mistakes continue throughout the presentation practice video, so pay special attention to them :";
                TextBox_Feedback.Text += "\r\n\r\n" + theWorstMistake;
            }

            TextBox_Feedback.Text += "\r\n\r\nEach action is graded as follows:";
            TextBox_Feedback.Text += "\r\n\r\n" + mf;
            TextBox_Feedback.Text += "\r\n\r\n" + gs;
            TextBox_Feedback.Text += "\r\n\r\nGrading is based on the duration of the action as a percentage of the total duration of the presentation.";
            TextBox_Feedback.Text += "\r\n\r\nFor mistakes, less than or equal to 33.33% is A, less than or equal to 53.33% is B, less than or equal to 73.33 is C," +
                " less than or equal to 93.33 is D, and greater than 93.33 is F.";
            TextBox_Feedback.Text += "\r\n\r\nFor good actions, greater than or equal to 93.33 is A, greater than or equal to 73.33% is B, greater than or equal to 53.33% is C," +
                " greater than or equal to 33.33% is D, and less than 33.33% is F.";


        }

        // Implement the click event of the Save button
        // Implement Feedback saving
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            //string feedBackPath = @"C:\\Users\\Chufan Zhang\\Desktop\\Bachelorarbeit\\Bachelor_Code\\FeedBack";
            // Save the feedback in txt format in a folder named FeedBack based on the video ID
            string filePath = System.IO.Path.Combine(feedbackOrdnerPath, feedbackVideoId + ".txt");
            int count = 0;

            // Determine whether a Feedback file with same name alreadt exists, and if it exists, append a number to it
            while (File.Exists(filePath))
            {
                count++;
                filePath = System.IO.Path.Combine(feedbackOrdnerPath, feedbackVideoId + "_" + count + ".txt");
            }

            File.WriteAllText(filePath, TextBox_Feedback.Text);

        }


    }


    // Create a MistakeCheckBox class containing mistake and the state of the CheckBox
    public class MistakeCheckBox
    {
        public string Mistake { get; set; }
        public bool Checked { get; set; }
    }

    public class GoodStuffCheckBox
    {
        public string GoodStuff { get; set; }
        public bool Checked { get; set; }
    }


    // Deserialize data in Json format into correspondingdata structures
    public class Sessions
    {
        public List<Session> sessions { get; set; }
    }
    public class Session
    {
        public string start { get; set; }
        public string videoId { get; set; }
        public List<Sentence> sentences { get; set; }
        public List<Action> actions { get; set; }
        public bool scriptVisible { get; set; }
        public string end { get; set; }
    }

    public class Sentence
    {
        public bool wasIdentified { get; set; }
        public string sentence { get; set; }
        public string end { get; set; }
        public string start { get; set; }
    }

    public class Action
    {
        public string start { get; set; }
        public string end { get; set; }
        public double id { get; set; }
        public string logAction { get; set; }
        public bool mistake { get; set; }
    }

    // Create a VideoObject class containing the video's titel and path
    public class VideoObject
    {
        public string Titel { get; set; }
        public string VideoPath { get; set; }
    }


}
