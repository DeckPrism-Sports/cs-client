namespace DPSports.Entity.DTO
{
    /// <summary>
    /// Contains the result of an api request
    /// </summary>
    /// <remarks>
    /// The Content property contains the result of the request
    /// </remarks>
    /// <typeparam name="T">Generic Parameter of the Content property</typeparam>
    public class DataResult<T>
    {
        private T _result = default(T);
        private bool _success = false;//the default is false
        private string _exception;

        /// <summary>
        /// True if there was no errors in the request
        /// </summary>
        public bool Success
        {
            get { return _success; }
            set { _success = value; }
        }

        /// <summary>
        /// Contains the result content of the request
        /// </summary>
        public T Content
        {
            get { return _result; }
            set { _result = value; }
        }

        /// <summary>
        /// If Success is false this will contain the error
        /// </summary>
        public string Exception
        {
            get { return _exception; }
            set { _exception = value; }
        }

    }
}
