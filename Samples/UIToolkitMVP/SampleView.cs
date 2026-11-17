using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// Owns the UIDocument. Queries elements, exposes events for the presenter,
    /// and exposes methods for the presenter to call. No business logic here.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SampleView : MonoBehaviour
    {
        #region Fields

        private UIDocument _document;
        private Label _countLabel;
        private Button _incrementButton;

        private SamplePresenter _presenter;

        #endregion

        #region Events

        public event Action OnIncrementClicked;

        #endregion

        #region MonoBehaviour Methods

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _countLabel = root.Q<Label>("count-label");
            _incrementButton = root.Q<Button>("increment-button");

            _incrementButton.clicked += HandleIncrementClicked;

            _presenter = new SamplePresenter(this);
        }

        private void OnDisable()
        {
            _incrementButton.clicked -= HandleIncrementClicked;
            _presenter?.Dispose();
            _presenter = null;
        }

        #endregion

        #region Public Methods

        public void SetCount(int count)
        {
            if (_countLabel != null)
            {
                _countLabel.text = count.ToString();
            }
        }

        #endregion

        #region Private Methods

        private void HandleIncrementClicked()
        {
            OnIncrementClicked?.Invoke();
        }

        #endregion
    }
}
