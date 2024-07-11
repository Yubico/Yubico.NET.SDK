// Copyright 2023 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#if WINDOWS
using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Yubico.YubiKey;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
#pragma warning disable CA1303
    public delegate void VoidVoidDel();

    public delegate void VoidStringDel(string message);

    public delegate void VoidStringKedDel(string message, KeyEntryData keyEntryData);

    public delegate bool BoolKedDel(KeyEntryData keyEntryData);

    public sealed class Fido2SampleGui : IDisposable
    {
        private readonly ParentForm _parentForm;
        private bool _disposed;

        public Fido2SampleGui()
        {
            _parentForm = new ParentForm();
            ReaderWriter.ReadLine = ReadFromAnswerBox;
            ReaderWriter.WriteLine = WriteToMenuBox;
        }

        public void RunSample()
        {
            using var tokenSource = new CancellationTokenSource();
            var sampleRunTask = Task.Run(() => CreateSampleRunThread(), tokenSource.Token);
            _ = _parentForm.ShowDialog();
            tokenSource.Cancel();
        }

        private void CreateSampleRunThread()
        {
            var keyCollector = new PopupWindowKeyCollector(_parentForm);
            var sample = new Fido2SampleRun(2, keyCollector);

            while (_parentForm.Answered)
            {
                Thread.Sleep(250);
            }

            sample.RunSample(false);
            _parentForm.CallClose();
        }

        public void WriteToMenuBox(string message)
        {
            string updatedMessage = UpdateNewLine(message);
            _parentForm.WriteMenuBox(updatedMessage);
        }

        public string ReadFromAnswerBox()
        {
            _parentForm.InitAnswerBox();
            while (!_parentForm.Answered)
            {
                Thread.Sleep(500);
            }

            return _parentForm.Answer;
        }

        public static string UpdateNewLine(string message)
        {
            if (message is null)
            {
                return "";
            }

            // Replace all instances of "\n" to "\r\n".
            string m1 = message.Replace("\n", "\r\n", StringComparison.Ordinal);
            // Maybe one of the new lines was already "\r\n", so we now have
            // "\r\r\n". So replace that with "\r\n".
            return m1.Replace("\r\r\n", "\r\n", StringComparison.Ordinal);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _parentForm.Dispose();

            _disposed = true;
        }
    }

    public class ParentForm : Form
    {
        private readonly TextBox _menuBox;
        private readonly Label _label;
        private readonly TextBox _answerBox;
        private readonly Button _cancelButton;
        private readonly FpTouchPopupForm _fpTouchPopupForm;
        private readonly PinPopupForm _pinPopupForm;

        private bool _disposed;

        public bool Answered { get; private set; }

        public string Answer { get; private set; }

        public ParentForm()
        {
            Answered = true;
            Text = "FIDO2 Sample";
            Size = new Size(600, 660);
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            _menuBox = new TextBox()
            {
                Size = new Size(540, 440),
                Multiline = true,
                ReadOnly = true,
                Location = new Point(20, 20),
                ScrollBars = ScrollBars.Vertical,
            };
            _label = new Label()
            {
                Text = "Enter answer here, then press Enter.",
                AutoSize = true,
                Location = new Point(20, 480),
            };
            _answerBox = new TextBox()
            {
                Size = new Size(250, 100),
                ReadOnly = true,
                Location = new Point(20, 510),
            };
            _cancelButton = new Button()
            {
                Text = "Cancel",
                Location = new Point(480, 550),
            };
            _fpTouchPopupForm = new FpTouchPopupForm("No action needed");
            _pinPopupForm = new PinPopupForm("No action needed");

            _cancelButton.Click += new EventHandler(CancelButton_Click);
            _answerBox.KeyDown += new KeyEventHandler(AnswerBox_KeyDown);

            Controls.Add(_menuBox);
            Controls.Add(_label);
            Controls.Add(_answerBox);
            Controls.Add(_cancelButton);
        }

        protected override void OnShown(EventArgs e)
        {
            Answered = false;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AnswerBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _menuBox.ReadOnly = false;
                _menuBox.Text += "\r\n" + _answerBox.Text + "\r\n";
                _menuBox.ReadOnly = true;
                _answerBox.ReadOnly = true;
                Answer = _answerBox.Text;
                Answered = true;
            }
        }

        public void WriteMenuBox(string message)
        {
            if (InvokeRequired)
            {
                _ = Invoke(new VoidStringDel(WriteMenuBox), message);
                return;
            }

            _menuBox.ReadOnly = false;
            _menuBox.Text += message + "\r\n";
            _menuBox.ReadOnly = true;
            _menuBox.SelectionStart = _menuBox.Text.Length;
            _menuBox.ScrollToCaret();
            _menuBox.Refresh();
            Refresh();
        }

        public void InitAnswerBox()
        {
            if (InvokeRequired)
            {
                _ = Invoke(new VoidVoidDel(InitAnswerBox));
                return;
            }

            _answerBox.Clear();
            Answered = false;
            _answerBox.ReadOnly = false;
            _ = _answerBox.Focus();
            Refresh();
        }

        public bool LaunchPinPopup(KeyEntryData keyEntryData)
        {
            if (InvokeRequired)
            {
                object result = Invoke(new BoolKedDel(LaunchPinPopup), keyEntryData);
                if (result is bool returnValue)
                {
                    return returnValue;
                }

                return false;
            }

            DialogResult dResult = DialogResult.OK;
            do
            {
                _pinPopupForm.UpdateMessage(dResult, keyEntryData);
                _pinPopupForm.Refresh();
                dResult = _pinPopupForm.ShowDialog(this);
            } while (dResult == DialogResult.Retry);

            return dResult == DialogResult.OK;
        }

        public void LaunchFpTouchPopup(string message, KeyEntryData keyEntryData)
        {
            if (InvokeRequired)
            {
                _ = Invoke(new VoidStringKedDel(LaunchFpTouchPopup), message, keyEntryData);
                return;
            }

            _fpTouchPopupForm.UpdateMessage(message, keyEntryData);
            _fpTouchPopupForm.Refresh();
            if (!_fpTouchPopupForm.Visible)
            {
                _fpTouchPopupForm.Show(this);
            }
        }

        public void CloseFpTouchPopup()
        {
            if (InvokeRequired)
            {
                _ = Invoke(new VoidVoidDel(CloseFpTouchPopup));
                return;
            }

            _fpTouchPopupForm.Hide();
        }

        public void CallClose()
        {
            if (InvokeRequired)
            {
                _ = Invoke(new VoidVoidDel(CallClose));
                return;
            }

            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _menuBox.Dispose();
                _label.Dispose();
                _answerBox.Dispose();
                _cancelButton.Dispose();
                _fpTouchPopupForm.Dispose();
                _pinPopupForm.Dispose();
            }

            _disposed = true;

            base.Dispose(true);
        }
    }

    public class PinPopupForm : Form
    {
        private readonly Label _message;
        private readonly Label _pinBoxLabel;
        private readonly TextBox _pinBox;
        private readonly Label _newPinBoxLabel;
        private readonly TextBox _newPinBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;

        private readonly Memory<byte> _currentPin;
        private readonly Memory<byte> _newPin;

        private int _currentPinLen;
        private int _newPinLen;

        // _state bits are
        //   1  one PIN requested
        //   2  two PINs requested (change)
        //   4  SetPin
        // For OnShown, if 1 or 2, set Focus to _current, if 4, _new
        // For UpdatePinBox, if Enter:
        //     1 or 4 goes to OK button
        //     2 sets focus to _new and set state to 3
        //     3 goes to OK button
        private int _state;

        private KeyEntryData _keyEntryData;

        private bool _disposed;

        public PinPopupForm(string message)
        {
            Text = "User Action Required";
            Size = new Size(400, 400);
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = true;
            StartPosition = FormStartPosition.CenterScreen;

            _message = new Label
            {
                Text = message,
                Location = new Point(20, 20),
                AutoSize = true
            };
            _pinBoxLabel = new Label
            {
                Text = "Current PIN:",
                Location = new Point(20, 75),
                AutoSize = true
            };
            _pinBox = new TextBox()
            {
                Size = new Size(250, 100),
                ReadOnly = true,
                Location = new Point(20, 100),
                PasswordChar = '*',
            };
            _newPinBoxLabel = new Label
            {
                Text = "New PIN:",
                Location = new Point(20, 175),
                AutoSize = true
            };
            _newPinBox = new TextBox()
            {
                Size = new Size(250, 100),
                ReadOnly = true,
                Location = new Point(20, 200),
                PasswordChar = '*',
            };
            _okButton = new Button()
            {
                Text = "OK",
                Location = new Point(180, 300),
            };
            _cancelButton = new Button()
            {
                Text = "Cancel",
                Location = new Point(280, 300),
            };

            _okButton.Click += new EventHandler(OkButton_Click);
            _cancelButton.Click += new EventHandler(CancelButton_Click);
            _pinBox.KeyDown += new KeyEventHandler(PinBox_KeyDown);
            _newPinBox.KeyDown += new KeyEventHandler(NewPinBox_KeyDown);

            Controls.Add(_message);
            Controls.Add(_pinBoxLabel);
            Controls.Add(_pinBox);
            Controls.Add(_newPinBoxLabel);
            Controls.Add(_newPinBox);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);

            _currentPin = new Memory<byte>(new byte[63]);
            _newPin = new Memory<byte>(new byte[63]);
        }

        protected override void OnShown(EventArgs e)
        {
            _ = _state < 3 ? _pinBox.Focus() : _newPinBox.Focus();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (!(_keyEntryData is null))
            {
                if (_keyEntryData.Request == KeyEntryRequest.ChangeFido2Pin)
                {
                    _keyEntryData.SubmitValues(_currentPin.Slice(0, _currentPinLen).Span, _newPin.Slice(0, _newPinLen).Span);
                }
                else if (_keyEntryData.Request == KeyEntryRequest.SetFido2Pin)
                {
                    _keyEntryData.SubmitValue(_newPin.Slice(0, _newPinLen).Span);
                }
                else
                {
                    _keyEntryData.SubmitValue(_currentPin.Slice(0, _currentPinLen).Span);
                }
            }

            EndPinPopup(DialogResult.OK);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            EndPinPopup(DialogResult.Cancel);
        }

        private void EndPinPopup(DialogResult dResult)
        {
            CryptographicOperations.ZeroMemory(_currentPin.Span);
            CryptographicOperations.ZeroMemory(_newPin.Span);
            _currentPinLen = 0;
            _newPinLen = 0;
            DialogResult = dResult;
            Close();
        }

        private void PinBox_KeyDown(object sender, KeyEventArgs e)
        {
            _currentPinLen += UpdatePinBox(e, _currentPin.Span, _currentPinLen);
        }

        private void NewPinBox_KeyDown(object sender, KeyEventArgs e)
        {
            _newPinLen += UpdatePinBox(e, _newPin.Span, _newPinLen);
        }

        // Return 0 if the char is not added to the pinBuffer.
        // Return 1 if the char is added.
        private int UpdatePinBox(KeyEventArgs e, Span<byte> pinBuffer, int currentLen)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (_state == 2)
                {
                    _state = 3;
                    _pinBox.ReadOnly = true;
                    _newPinBox.ReadOnly = false;
                    _newPinLen = 0;
                    _ = _newPinBox.Focus();
                    return 0;
                }

                OkButton_Click(this, new EventArgs());
                return 0;
            }

            if (e.KeyValue < 0x21 || e.KeyValue > 0x7E || currentLen >= 63)
            {
                EndPinPopup(DialogResult.Retry);
                return 0;
            }

            pinBuffer[currentLen] = (byte)e.KeyValue;
            _okButton.Enabled = true;

            return 1;
        }

        public void UpdateMessage(DialogResult currentState, KeyEntryData keyEntryData)
        {
            _pinBox.Clear();
            _newPinBox.Clear();
            _state = 1;

            _message.Text =
                "This sample accepts only ASCII input and the\n" +
                "maximum PIN length is 63 characters.\n";
            if (!(keyEntryData is null))
            {
                _keyEntryData = keyEntryData;
                if (!(keyEntryData.RetriesRemaining is null))
                {
                    _message.Text =
                        "\nPrevious PIN was incorrect." +
                        "\nNumber of retries remaining: " +
                        keyEntryData.RetriesRemaining + "\n\n";
                }

                if (keyEntryData.Request == KeyEntryRequest.SetFido2Pin)
                {
                    _pinBoxLabel.Visible = false;
                    _pinBox.Visible = false;
                    _pinBox.ReadOnly = true;
                    _newPinBoxLabel.Visible = true;
                    _newPinBox.Visible = true;
                    _newPinBox.ReadOnly = false;
                    _state = 4;
                }
                else if (keyEntryData.Request == KeyEntryRequest.ChangeFido2Pin)
                {
                    _pinBoxLabel.Visible = true;
                    _pinBox.Visible = true;
                    _pinBox.ReadOnly = false;
                    _newPinBoxLabel.Visible = true;
                    _newPinBox.Visible = true;
                    _newPinBox.ReadOnly = false;
                    _state = 2;
                }
                else
                {
                    _pinBoxLabel.Visible = true;
                    _pinBox.Visible = true;
                    _pinBox.ReadOnly = false;
                    _newPinBoxLabel.Visible = false;
                    _newPinBox.Visible = false;
                    _newPinBox.ReadOnly = true;
                }
            }

            if (currentState == DialogResult.Retry)
            {
                _message.Text = "Invalid input, try again.\n\n";
            }

            _currentPinLen = 0;
            _newPinLen = 0;
            _okButton.Enabled = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _message.Dispose();
                _pinBoxLabel.Dispose();
                _pinBox.Dispose();
                _newPinBoxLabel.Dispose();
                _newPinBox.Dispose();
                _okButton.Dispose();
                _cancelButton.Dispose();
            }

            _disposed = true;

            base.Dispose(true);
        }
    }

    public class FpTouchPopupForm : Form
    {
        private readonly Label _message;
        private readonly Button _cancelButton;
        private bool _disposed;

        private SignalUserCancel _cancelCallback;

        public FpTouchPopupForm(string message)
        {
            Text = "User Action Required";
            Size = new Size(400, 400);
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = true;
            StartPosition = FormStartPosition.CenterScreen;

            _message = new Label
            {
                Text = message,
                Location = new Point(20, 20),
                AutoSize = true
            };
            _cancelButton = new Button()
            {
                Text = "Cancel",
                Location = new Point(280, 300),
            };
            _cancelCallback = CancelUnavailable;

            _cancelButton.Click += new EventHandler(CancelButton_Click);

            Controls.Add(_message);
            Controls.Add(_cancelButton);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            _cancelCallback();
        }

        private void CancelUnavailable()
        {
            _message.Text = "User cancellation is currently unavailable.";
            Refresh();
        }

        public void UpdateMessage(string message, KeyEntryData keyEntryData)
        {
            _message.Text = "";
            if (!(keyEntryData is null))
            {
                if (!(keyEntryData.LastBioEnrollSampleResult is null))
                {
                    _message.Text =
                        "Sample result: " +
                        keyEntryData.LastBioEnrollSampleResult.LastEnrollSampleStatus.ToString() +
                        "\nNumber of good samples still needed: " +
                        keyEntryData.LastBioEnrollSampleResult.RemainingSampleCount + "\n\n";
                }

                if (!(keyEntryData.SignalUserCancel is null))
                {
                    _cancelCallback = keyEntryData.SignalUserCancel;
                }
            }

            _message.Text += message;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _message.Dispose();
                _cancelButton.Dispose();
            }

            _disposed = true;

            base.Dispose(true);
        }
    }
#pragma warning restore CA1303
}
#endif
