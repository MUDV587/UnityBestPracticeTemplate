using System;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// Owns model data and drives the view. Has no Unity dependencies.
    /// </summary>
    public class SamplePresenter
    {
        #region Fields

        private int _count;
        private readonly SampleView _view;

        #endregion

        #region Events

        public event Action<int> CountChanged;

        #endregion

        #region Properties

        public int Count => _count;

        #endregion

        #region Public Methods

        public SamplePresenter(SampleView view)
        {
            _view = view;
            _view.OnIncrementClicked += HandleIncrementClicked;
        }

        public void Dispose()
        {
            _view.OnIncrementClicked -= HandleIncrementClicked;
        }

        #endregion

        #region Private Methods

        private void HandleIncrementClicked()
        {
            _count++;
            _view.SetCount(_count);
            CountChanged?.Invoke(_count);
        }

        #endregion
    }
}
