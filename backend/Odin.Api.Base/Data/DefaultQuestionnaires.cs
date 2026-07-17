using System.Text.Json;

namespace Odin.Api.Base.Data;

/// <summary>
/// Ready-made evaluation questionnaires seeded once by name (admins may edit
/// or delete them in the builder afterwards; the seeder never overwrites).
/// Design rules: max 15 questions each, mostly 5-point Likert statements so
/// results aggregate into statistics, one 0-10 recommend score per
/// questionnaire, and a couple of free-text questions for qualitative data.
/// </summary>
public static class DefaultQuestionnaires
{
    private static object T(string fallback) => new { fallback };

    private static readonly object[] Likert5 =
    [
        new { value = "1", label = "Strongly disagree" },
        new { value = "2", label = "Disagree" },
        new { value = "3", label = "Neutral" },
        new { value = "4", label = "Agree" },
        new { value = "5", label = "Strongly agree" },
    ];

    private static object[] Scale0To10() =>
        Enumerable.Range(0, 11).Select(n => (object)new { value = n.ToString(), label = n.ToString() }).ToArray();

    private static object Likert(string id, string label) =>
        new { id, type = "radio", label = T(label), required = true, props = new { options = Likert5 } };

    private static object Recommend(string id, string label) =>
        new { id, type = "select", label = T(label), required = true, props = new { options = Scale0To10(), placeholder = "0 = not at all likely · 10 = extremely likely" } };

    private static object Select(string id, string label, params string[] options) =>
        new
        {
            id, type = "select", label = T(label), required = true,
            props = new { options = options.Select(o => (object)new { value = o, label = o }).ToArray(), placeholder = "— select —" },
        };

    private static object Radio(string id, string label, params string[] options) =>
        new
        {
            id, type = "radio", label = T(label), required = true,
            props = new { options = options.Select(o => (object)new { value = o, label = o }).ToArray() },
        };

    private static object Text(string id, string label, bool required = false, string placeholder = "") =>
        new { id, type = "text", label = T(label), required, props = new { placeholder } };

    private static object TextArea(string id, string label, bool required = false, string placeholder = "") =>
        new { id, type = "textarea", label = T(label), required, props = new { placeholder, rows = 3 } };

    private static string Build(string id, string name, string description, object[] items)
    {
        var q = new
        {
            version = "1.0.0",
            id,
            name = T(name),
            description = T(description),
            pages = new object[]
            {
                new
                {
                    id = id + "-p1",
                    title = T(name),
                    sections = new object[]
                    {
                        new
                        {
                            id = id + "-s1",
                            groups = new object[] { new { id = id + "-g1", items } },
                        },
                    },
                },
            },
        };
        return JsonSerializer.Serialize(q);
    }

    public static IReadOnlyList<(string Name, string Json)> All()
    {
        var list = new List<(string, string)>
        {
            ("Student Evaluation — School & Services", Build("se-school",
                "Student Evaluation — School & Services",
                "How well do the school and its student services work for you? Anonymous statistics help us improve.",
                [
                    Likert("sch-admission",  "The admission and enrolment process was smooth and well communicated."),
                    Likert("sch-response",   "The school's staff respond quickly when I need help."),
                    Likert("sch-info",       "Information about schedules, deadlines and requirements is clear."),
                    Likert("sch-materials",  "The study materials I receive are professional and useful."),
                    Likert("sch-documents",  "My documents, letters and certificates are handled correctly and on time."),
                    Likert("sch-payment",    "Payment and invoicing information is clear and correct."),
                    Likert("sch-welcome",    "I feel welcome and respected as a student at the school."),
                    Likert("sch-progress",   "The school keeps me well informed about my study progress."),
                    Likert("sch-promise",    "The school lives up to what I was promised when I enrolled."),
                    Select("sch-contact",    "How often do you contact the school?",
                        "Weekly or more", "A few times a month", "Rarely", "Never"),
                    Recommend("sch-nps",     "How likely are you to recommend the school to a friend or colleague? (0-10)"),
                    TextArea("sch-improve",  "What should the school improve first?"),
                    TextArea("sch-best",     "What do you value most about the school?"),
                ])),

            ("Student Evaluation — Education & Programme", Build("se-edu",
                "Student Evaluation — Education & Programme",
                "Your honest view on the programme content, workload and value.",
                [
                    Likert("edu-expectations", "The programme content matches my expectations."),
                    Likert("edu-level",        "The level of difficulty is right for me."),
                    Likert("edu-workload",     "The workload per module is manageable next to my job and life."),
                    Likert("edu-structure",    "The modules build on each other in a logical order."),
                    Likert("edu-relevance",    "Assignments and projects are relevant to my professional work."),
                    Likert("edu-feedback",     "The feedback I get on assignments helps me improve."),
                    Likert("edu-grading",      "Grading feels fair and transparent."),
                    Likert("edu-career",       "What I learn is directly useful in my career."),
                    Likert("edu-value",        "The programme is worth the tuition I pay."),
                    Select("edu-mostvalue",    "Which part of the programme gives you the most value?",
                        "Study materials", "Assignments", "Projects", "Thesis / final project", "Contact with teachers"),
                    Select("edu-pace",         "How is the study pace for you?",
                        "Too slow", "Just right", "Too fast"),
                    TextArea("edu-improve",    "Which topic or module should be improved — and how?"),
                    TextArea("edu-missing",    "Is any topic missing from the programme?"),
                ])),

            ("Student Evaluation — Teacher", Build("se-teacher",
                "Student Evaluation — Teacher",
                "Evaluate one teacher for one module. Fill it once per teacher you want to rate.",
                [
                    Text("tea-who",        "Which teacher and module are you evaluating?", required: true,
                        placeholder: "e.g. John Smith — BBA-101 Business Environment"),
                    Likert("tea-clarity",  "The teacher explains topics clearly."),
                    Likert("tea-prepared", "The teacher is well prepared."),
                    Likert("tea-response", "The teacher answers questions within a reasonable time."),
                    Likert("tea-feedback", "The teacher's feedback is constructive and specific."),
                    Likert("tea-engaging", "The teacher makes the subject engaging."),
                    Likert("tea-respect",  "The teacher treats students with respect."),
                    Likert("tea-practice", "The teacher's professional experience enriches the teaching."),
                    Likert("tea-overall",  "Overall, this teacher meets my expectations."),
                    Select("tea-contact",  "How often do you interact with this teacher?",
                        "Weekly or more", "A few times a month", "Only through assignment feedback", "Almost never"),
                    TextArea("tea-keep",   "What should this teacher keep doing?"),
                    TextArea("tea-change", "What should this teacher change?"),
                ])),

            ("Student Evaluation — Moodle (LMS)", Build("se-moodle",
                "Student Evaluation — Moodle (LMS)",
                "How well does our online learning platform work for you?",
                [
                    Likert("lms-login",     "Logging in to Moodle works without problems."),
                    Likert("lms-find",      "I can easily find my course materials."),
                    Likert("lms-upload",    "Uploading assignments works reliably."),
                    Likert("lms-mobile",    "Moodle works well on my phone or tablet."),
                    Likert("lms-structure", "The structure of my courses in Moodle is clear."),
                    Likert("lms-notify",    "Notifications keep me up to date on what I need to do."),
                    Likert("lms-overall",   "Overall, Moodle supports my studies well."),
                    Select("lms-frequency", "How often do you use Moodle?",
                        "Daily", "Several times a week", "About weekly", "Less than weekly"),
                    Select("lms-device",    "Which device do you mainly use for Moodle?",
                        "Laptop / desktop", "Phone", "Tablet"),
                    TextArea("lms-problem", "What is the most annoying problem you meet in Moodle?"),
                    TextArea("lms-wish",    "What one improvement would help you the most?"),
                ])),

            ("Student Evaluation — Overall Satisfaction", Build("se-overall",
                "Student Evaluation — Overall Satisfaction",
                "Two minutes: your overall verdict on studying with us.",
                [
                    Recommend("all-nps",    "How likely are you to recommend us to a friend or colleague? (0-10)"),
                    Likert("all-positive",  "My overall experience as a student is positive."),
                    Likert("all-goals",     "The education is helping me reach my career goals."),
                    Likert("all-value",     "I get good value for the money I invest."),
                    Likert("all-complete",  "I intend to complete my programme here."),
                    Select("all-again",     "Would you consider taking another programme with us after this one?",
                        "Yes", "Maybe", "No"),
                    TextArea("all-best",    "In one sentence: what do we do best?"),
                    TextArea("all-fix",     "In one sentence: what should we fix first?"),
                ])),

            ("Career & Recruitment Data", Build("rd-career",
                "Career & Recruitment Data",
                "Tell us about your career so we can document the value of the education — and, if you allow it, connect you with international recruitment opportunities.",
                [
                    Select("car-status",     "What is your current employment status?",
                        "Employed full-time", "Employed part-time", "Self-employed / business owner", "Looking for work", "Not working at the moment"),
                    Text("car-title",        "What is your current job title?", placeholder: "e.g. Operations Manager"),
                    Select("car-industry",   "Which industry do you work in?",
                        "Banking & Finance", "IT & Software", "Healthcare", "Education", "Manufacturing", "Retail & Trade",
                        "Logistics & Transport", "Hospitality & Tourism", "Government & NGO", "Construction & Real Estate", "Other"),
                    Select("car-change",     "Has your job situation changed since starting the programme?",
                        "Promoted", "More responsibility (same title)", "Changed to a better employer", "Started my own business", "Unchanged"),
                    Select("car-salary",     "How has your salary developed since starting the programme?",
                        "Increased more than 25%", "Increased 10-25%", "Increased up to 10%", "Unchanged", "Decreased", "Prefer not to say"),
                    Select("car-funding",    "Who pays for your education?",
                        "Myself", "My employer (fully)", "My employer (partly)", "Family", "Scholarship / other"),
                    Select("car-decisive",   "What was the most decisive factor when you chose this programme?",
                        "Price", "Flexibility / online study", "Accreditation & recognition", "Time to complete", "Recommendation from my learning centre", "Programme content"),
                    TextArea("car-why",      "In your own words: why did you choose this programme?"),
                    Radio("car-remote",      "Are you interested in a remote job with a company in another country?",
                        "Yes, actively looking", "Yes, open to offers", "No"),
                    Radio("car-relocate",    "Would you consider relocating to another country for work?",
                        "Yes, actively looking", "Yes, open to it", "No"),
                    Text("car-countries",    "If yes: which countries would interest you?", placeholder: "e.g. Germany, UAE, Canada"),
                    Radio("car-share",       "May we share your profile and career data with our recruitment partners and platforms to match you with job opportunities?",
                        "Yes", "No"),
                    Radio("car-marketing",   "May we use your name and success story in our marketing?",
                        "Yes", "Yes, anonymised only", "No"),
                    TextArea("car-testimonial", "Write a short testimonial about your study experience (optional)."),
                    Recommend("car-nps",     "How likely are you to recommend this education to a colleague? (0-10)"),
                ])),
        };
        return list;
    }
}
