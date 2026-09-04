using UnityEngine;

namespace SP2FreeCamera
{
    internal static class FreeCameraGuiTheme
    {
        private static GUIStyle _windowStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _centeredLabelStyle;
        private static GUIStyle _statusStyle;
        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _toggleStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _textFieldStyle;
        private static GUIStyle _smallButtonStyle;

        private static Texture2D _windowBackground;
        private static Texture2D _buttonBackground;
        private static Texture2D _buttonHoverBackground;

        internal static GUIStyle WindowStyle
        {
            get
            {
                EnsureStyles();
                return _windowStyle;
            }
        }

        internal static GUIStyle LabelStyle
        {
            get
            {
                EnsureStyles();
                return _labelStyle;
            }
        }

        internal static GUIStyle CenteredLabelStyle
        {
            get
            {
                EnsureStyles();
                return _centeredLabelStyle;
            }
        }

        internal static GUIStyle StatusStyle
        {
            get
            {
                EnsureStyles();
                return _statusStyle;
            }
        }

        internal static GUIStyle SectionHeaderStyle
        {
            get
            {
                EnsureStyles();
                return _sectionHeaderStyle;
            }
        }

        internal static GUIStyle ToggleStyle
        {
            get
            {
                EnsureStyles();
                return _toggleStyle;
            }
        }

        internal static GUIStyle ButtonStyle
        {
            get
            {
                EnsureStyles();
                return _buttonStyle;
            }
        }

        internal static GUIStyle TextFieldStyle
        {
            get
            {
                EnsureStyles();
                return _textFieldStyle;
            }
        }

        internal static GUIStyle SmallButtonStyle
        {
            get
            {
                EnsureStyles();
                return _smallButtonStyle;
            }
        }

        internal static void EnsureStyles()
        {
            if (_windowStyle != null)
            {
                return;
            }

            _windowBackground = MakeTexture(new Color(0.04f, 0.045f, 0.055f, 0.92f));
            _buttonBackground = MakeTexture(new Color(0.12f, 0.15f, 0.18f, 0.96f));
            _buttonHoverBackground = MakeTexture(new Color(0.18f, 0.24f, 0.30f, 1f));

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 15,
                padding = new RectOffset(14, 14, 24, 14),
                normal =
                {
                    background = _windowBackground,
                    textColor = Color.white
                },
                onNormal =
                {
                    background = _windowBackground,
                    textColor = Color.white
                },
                focused =
                {
                    background = _windowBackground,
                    textColor = Color.white
                },
                active =
                {
                    background = _windowBackground,
                    textColor = Color.white
                }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal =
                {
                    textColor = new Color(0.96f, 0.98f, 1f, 1f)
                }
            };

            _centeredLabelStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            _statusStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 13,
                normal =
                {
                    textColor = new Color(1f, 0.92f, 0.45f, 1f)
                }
            };

            _sectionHeaderStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = new Color(0.62f, 0.86f, 1f, 1f)
                }
            };

            _toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 14,
                wordWrap = true,
                normal =
                {
                    textColor = Color.white
                },
                hover =
                {
                    textColor = new Color(1f, 0.96f, 0.62f, 1f)
                },
                active =
                {
                    textColor = Color.white
                },
                focused =
                {
                    textColor = Color.white
                },
                onNormal =
                {
                    textColor = Color.white
                },
                onHover =
                {
                    textColor = new Color(1f, 0.96f, 0.62f, 1f)
                },
                onActive =
                {
                    textColor = Color.white
                },
                onFocused =
                {
                    textColor = Color.white
                }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                wordWrap = true,
                fixedHeight = 40f,
                margin = new RectOffset(0, 0, 2, 2),
                normal =
                {
                    background = _buttonBackground,
                    textColor = Color.white
                },
                hover =
                {
                    background = _buttonHoverBackground,
                    textColor = Color.white
                },
                active =
                {
                    background = _buttonHoverBackground,
                    textColor = Color.white
                },
                focused =
                {
                    background = _buttonBackground,
                    textColor = Color.white
                }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                fixedHeight = 27f,
                wordWrap = false,
                normal =
                {
                    background = _buttonBackground,
                    textColor = Color.white
                },
                hover =
                {
                    background = _buttonHoverBackground,
                    textColor = Color.white
                },
                focused =
                {
                    background = _buttonHoverBackground,
                    textColor = Color.white
                },
                active =
                {
                    background = _buttonHoverBackground,
                    textColor = Color.white
                }
            };

            _smallButtonStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0f,
                alignment = TextAnchor.MiddleCenter
            };
        }

        internal static void Release()
        {
            DestroyTexture(ref _windowBackground);
            DestroyTexture(ref _buttonBackground);
            DestroyTexture(ref _buttonHoverBackground);
            _windowStyle = null;
            _labelStyle = null;
            _centeredLabelStyle = null;
            _statusStyle = null;
            _sectionHeaderStyle = null;
            _toggleStyle = null;
            _buttonStyle = null;
            _textFieldStyle = null;
            _smallButtonStyle = null;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture != null)
            {
                Object.Destroy(texture);
                texture = null;
            }
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
