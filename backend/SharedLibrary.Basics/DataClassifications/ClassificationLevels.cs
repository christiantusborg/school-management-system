using System.ComponentModel;

namespace QuVian.SharedLibrary.Basics.DataClassifications
{
    /// <summary>
    /// Defines various levels of data classification to ensure appropriate handling and protection
    /// based on sensitivity and compliance requirements.
    /// </summary>
    public enum ClassificationLevels
    {
        /// <summary>
        /// Public: Information that does not require any access restrictions.
        /// Suitable for data that can be made available to the general public without any risk, such as
        /// public web pages or publicly released company announcements.
        /// </summary>
        [Description("Public information with no access restrictions.")]
        Public,

        /// <summary>
        /// Internal: Information restricted to authorized personnel within the organization.
        /// Intended for internal documents, internal project details, and operational data that
        /// should not be exposed outside of the company infrastructure.
        /// </summary>
        [Description("Internal use only; restricted to authorized personnel.")]
        Internal,

        /// <summary>
        /// Sensitive: Personal and sensitive information that requires protection under
        /// regulations such as GDPR and HIPAA. This includes any data that can directly or
        /// indirectly identify an individual, requiring careful handling to prevent privacy breaches.
        /// </summary>
        [Description("Personal and sensitive information requiring high levels of protection under regulations like GDPR and HIPAA.")]
        Sensitive,

        /// <summary>
        /// Confidential: Data that, if disclosed, could cause significant harm or requires adherence
        /// to strict regulatory compliance. This level is used for information that is critical to
        /// business operations or could lead to legal or financial repercussions if mishandled.
        /// </summary>
        [Description("Highly confidential data that, if disclosed, could cause significant harm or requires strict regulatory compliance.")]
        Confidential,

        /// <summary>
        /// HighlySensitive: Data involving health, racial, ethnic, genetic, or biometric details
        /// that require the highest protection levels under laws like GDPR and HIPAA.
        /// Such data is often targeted in data breaches for its sensitivity, necessitating stringent
        /// security measures and compliance procedures.
        /// </summary>
        [Description("Data that involves health, racial, ethnic, genetic, or biometric details requiring the highest protection levels under laws like GDPR and HIPAA.")]
        HighlySensitive,
        SensitivePersonalDataSystemUsage,
        InternalSystemUsage
    }
}