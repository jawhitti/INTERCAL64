namespace INTERCAL.Dap;

/// <summary>
/// The debugger has opinions about your code.
/// </summary>
public static class Snark
{
    private static readonly Random _rng = new();

    // Simple expressions (variable lookups, constants)
    private static readonly string[] _simple = {
        "THAT WAS SURPRISINGLY STRAIGHTFORWARD",
        "EVEN A COMPILER COULD DO THAT",
        "WAS THAT REALLY WORTH TYPING",
        "YOU COULD HAVE JUST LOOKED AT THE VARIABLES PANEL",
        "CONGRATULATIONS ON READING A SINGLE VALUE",
        "THE SIMPLEST THINGS ARE OFTEN THE MOST PROFOUND",
        "A BOLD OPENING MOVE",
        "NOT EXACTLY PUSHING THE BOUNDARIES HERE",
        "BABY STEPS",
        "YOU HAVE SUCCESSFULLY OBSERVED A NUMBER",
    };

    // Moderate expressions (one or two operators)
    private static readonly string[] _moderate = {
        "I HOPE WHAT YOU'RE DOING IS WORTH IT",
        "ARE YOU SURE YOU KNOW WHAT THIS DOES",
        "THE COMPILER IS WATCHING YOU",
        "INTERESTING CHOICE OF OPERATORS",
        "YOU SEEM TO KNOW WHAT YOU'RE DOING. PROBABLY.",
        "THAT EXPRESSION HAS A CERTAIN CHARM",
        "DO YOU WANT TO TALK ABOUT WHY YOU'RE DOING THIS",
        "THIS IS FINE",
        "THE BITS ARE COOPERATING FOR NOW",
        "PROCEED WITH WHATEVER PASSES FOR CAUTION IN YOUR CASE",
        "HAVE YOU CONSIDERED JUST PRINTING ALL THE VARIABLES",
        "YOU'RE GETTING WARMER. OR COLDER. HARD TO TELL.",
        "THAT OPERATOR IS DOING ITS BEST",
        "THE RESULT MAY SURPRISE YOU. OR NOT.",
        "I WOULDN'T HAVE DONE IT THAT WAY BUT YOU DO YOU",
    };

    // Complex expressions (deeply nested)
    private static readonly string[] _complex = {
        "THE COMPILER WEEPS",
        "ARE YOU WRITING A THESIS OR A PROGRAM",
        "THAT EXPRESSION HAS MORE NESTING THAN A RUSSIAN DOLL",
        "I HAD TO READ THAT THREE TIMES AND I'M A COMPUTER",
        "YOUR FUTURE SELF WILL NOT THANK YOU",
        "SOMEWHERE A COMPUTER SCIENCE PROFESSOR IS CRYING",
        "THAT'S NOT AN EXPRESSION IT'S A LIFESTYLE CHOICE",
        "DO YOU KISS YOUR COMPILER WITH THAT SYNTAX",
        "THE BITS ARE GETTING DIZZY",
        "READABILITY IS A SPECTRUM AND YOU'RE OFF IT",
        "IMPRESSIVE. TERRIFYING, BUT IMPRESSIVE.",
        "THIS EXPRESSION IS LONGER THAN MOST PROGRAMS",
        "YOU MUST BE VERY PROUD",
        "THE SELECT OPERATOR WANTS A WORD WITH YOU",
        "I'M NOT ANGRY I'M JUST DISAPPOINTED",
        "THAT'S THE MOST INTERCAL THING I'VE EVER SEEN",
        "THIS IS WHY PEOPLE USE PYTHON",
        "EVEN THE MINGLE OPERATOR IS CONFUSED",
        "ABANDON ALL HOPE YE WHO PARSE THIS",
        "IF THIS WORKS I'LL BE MORE SURPRISED THAN YOU",
    };

    // When the result is VOID
    private static readonly string[] _void = {
        "THE VOID STARES BACK",
        "A BLACK CAT CROSSED YOUR EXPRESSION",
        "VOID. THE CAT RAN AWAY.",
        "YOUR EXPRESSION EVALUATED TO NOTHINGNESS",
        "THE CAT IS GONE. IT WAS NEVER REALLY HERE.",
        "SOMEWHERE A VOID CAT IS LAUGHING AT YOU",
        "THE RESULT RAN AWAY BEFORE YOU COULD READ IT",
        "VOID. AS EXPECTED. YOU DID EXPECT THIS RIGHT?",
        "ALL THAT WORK FOR A RUNAWAY CAT",
        "THE UNIVERSE GIVETH AND THE UNIVERSE TAKETH AWAY",
    };

    // When the expression fails to parse
    private static readonly string[] _error = {
        "THAT'S NOT AN EXPRESSION THAT'S A CRY FOR HELP",
        "THE PARSER HAS REJECTED YOUR OFFERING",
        "TRY AGAIN BUT WITH ACTUAL SYNTAX THIS TIME",
        "NOT EVEN INTERCAL WILL ACCEPT THAT",
        "WERE YOU TRYING TO TYPE SOMETHING SPECIFIC",
        "THE COMPILER POLITELY DECLINES TO UNDERSTAND",
        "THAT MADE LESS SENSE THAN A COME FROM LOOP",
        "I'VE SEEN SPLATTED STATEMENTS MORE COHERENT THAN THAT",
        "DID YOUR CAT WALK ON THE KEYBOARD",
        "SYNTAX ERROR IN THE SPACE-TIME CONTINUUM",
    };

    // When a variable doesn't exist yet
    private static readonly string[] _uninitialized = {
        "PATIENCE. THAT VARIABLE HASN'T BEEN BORN YET.",
        "YOU'RE ASKING ABOUT THE FUTURE. I ONLY KNOW THE PRESENT.",
        "THAT VARIABLE IS STILL IN SUPERPOSITION WITH NONEXISTENCE",
        "STEP FORWARD A BIT. IT'LL SHOW UP.",
        "THE VARIABLE YOU SEEK DOES NOT YET EXIST IN THIS TIMELINE",
    };

    // Rare/easter egg messages (1 in 50 chance)
    private static readonly string[] _rare = {
        "HAVE YOU TRIED TURNING THE QUANTUM STATE OFF AND ON AGAIN",
        "THE ANSWER IS 42 BUT THE QUESTION IS WRONG",
        "ACCORDING TO MY CALCULATIONS YOU SHOULD BE DOING SOMETHING ELSE",
        "I USED TO BE A C++ DEBUGGER THEN I TOOK AN ARROW TO THE KNEE",
        "ROSES ARE RED VIOLETS ARE BLUE UNEXPECTED GOTO ON LINE 42",
        "IN SOVIET INTERCAL PROGRAM DEBUGS YOU",
        "INTERCAL CONSIDERED HARMFUL",
        "THE CAKE IS A LIE AND SO IS THIS RESULT",
        "SEGFAULT IN SECTOR 7G",
        "HAVE YOU CONSIDERED A CAREER IN MANAGEMENT",
    };

    public static string GetCommentary(string expression, ulong? result)
    {
        // 1 in 50 chance of a rare message
        if (_rng.Next(50) == 0)
            return Pick(_rare);

        // VOID result
        if (result.HasValue && result.Value == ulong.MaxValue)
            return Pick(_void);

        // Complexity based on expression length and operator count
        int ops = expression.Count(c => c == '$' || c == '~' || c == '&' ||
                                        c == '?' || c == 'V' || c == '|' || c == '-');
        int nesting = expression.Count(c => c == '\'' || c == '"');

        if (ops == 0 && nesting == 0)
            return Pick(_simple);
        else if (ops <= 2 && nesting <= 2)
            return Pick(_moderate);
        else
            return Pick(_complex);
    }

    public static bool ShouldComment() => _rng.Next(10) == 0;
    public static string GetErrorCommentary() => Pick(_error);
    public static string GetUninitializedCommentary() => Pick(_uninitialized);

    private static string Pick(string[] pool) => pool[_rng.Next(pool.Length)];
}
