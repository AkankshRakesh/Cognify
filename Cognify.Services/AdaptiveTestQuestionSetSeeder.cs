using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Data;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Services
{
    public static class AdaptiveTestQuestionSetSeeder
    {
        private const string SetTitle = "Computer Science Adaptive Test Bank (120 Questions)";

        private static readonly QuestionSeed[] QuestionSeeds =
        {
            new("Which data structure follows last-in, first-out ordering?", "Stack", "Queue", "Heap", "Graph", DifficultyLevel.Easy),
            new("Which data structure is typically used to process items in arrival order?", "Queue", "Stack", "Tree", "Set", DifficultyLevel.Easy),
            new("Which structure stores elements in contiguous indexed positions?", "Array", "Linked list", "Trie", "Graph", DifficultyLevel.Easy),
            new("What is the average lookup time of a well-distributed hash table?", "O(1)", "O(log n)", "O(n)", "O(n log n)", DifficultyLevel.Medium),
            new("Which traversal of a binary search tree visits keys in sorted order?", "In-order", "Pre-order", "Post-order", "Level-order", DifficultyLevel.Medium),
            new("In a min-heap, where is the smallest element stored?", "At the root", "At the last leaf", "In the leftmost leaf", "At an arbitrary node", DifficultyLevel.Medium),
            new("Which structure is designed for efficient prefix lookups?", "Trie", "Stack", "Binary heap", "Disjoint set", DifficultyLevel.Medium),
            new("For a singly linked list, what is the cost of accessing the element at index n?", "O(n)", "O(1)", "O(log n)", "O(n log n)", DifficultyLevel.Hard),
            new("What is the worst-case search time in an unbalanced binary search tree?", "O(n)", "O(1)", "O(log n)", "O(√n)", DifficultyLevel.Hard),
            new("Which representation is usually space-efficient for a sparse graph?", "Adjacency list", "Adjacency matrix", "Full binary tree", "Sorted array", DifficultyLevel.Hard),

            new("What condition must hold before binary search can be used on a sequence?", "The sequence is sorted", "The sequence has even length", "All values are unique", "The sequence is linked", DifficultyLevel.Easy),
            new("Which algorithm repeatedly compares adjacent elements and swaps out-of-order pairs?", "Bubble sort", "Merge sort", "Binary search", "Dijkstra's algorithm", DifficultyLevel.Easy),
            new("Which algorithm explores a graph layer by layer?", "Breadth-first search", "Depth-first search", "Quicksort", "Prim's algorithm", DifficultyLevel.Easy),
            new("What is the time complexity of merge sort in the average case?", "O(n log n)", "O(n)", "O(n²)", "O(log n)", DifficultyLevel.Hard),
            new("Which technique solves a problem by combining solutions to overlapping subproblems?", "Dynamic programming", "Linear search", "Selection sort", "Randomized hashing", DifficultyLevel.Medium),
            new("Which shortest-path algorithm requires non-negative edge weights?", "Dijkstra's algorithm", "Bellman-Ford only", "Depth-first search", "Kruskal's algorithm", DifficultyLevel.Medium),
            new("What does a topological ordering require of its directed graph?", "It is acyclic", "It is complete", "It is weighted", "It is strongly connected", DifficultyLevel.Medium),
            new("What is quicksort's worst-case time complexity?", "O(n²)", "O(n log n)", "O(log n)", "O(n)", DifficultyLevel.Hard),
            new("Which algorithm finds a minimum spanning tree by repeatedly choosing the lightest crossing edge?", "Kruskal's algorithm", "Floyd-Warshall", "Binary search", "A* search", DifficultyLevel.Hard),
            new("Why can a greedy choice fail to produce a globally optimal solution?", "A locally best choice may prevent a better overall combination", "It always examines every permutation", "It requires negative memory", "It cannot compare numeric weights", DifficultyLevel.Hard),

            new("What is the purpose of a variable in a program?", "To name a value that can be used by the program", "To compile source code into machine code", "To encrypt a network connection", "To store a database schema only", DifficultyLevel.Easy),
            new("What is the base case in a recursive function used for?", "To stop recursion for a simple input", "To allocate a network socket", "To repeat the recursive call forever", "To sort the call stack", DifficultyLevel.Easy),
            new("Which programming construct selects between alternative execution paths?", "Conditional statement", "Array index", "Comment", "Type alias", DifficultyLevel.Easy),
            new("What does encapsulation primarily do in object-oriented design?", "Groups state with operations and controls access to them", "Runs several programs on separate machines", "Converts source code to SQL", "Guarantees every method is static", DifficultyLevel.Medium),
            new("What is polymorphism in object-oriented programming?", "Different types can be used through a shared interface", "A variable can store only one literal value", "A class must contain no methods", "A program executes without input", DifficultyLevel.Medium),
            new("What does a compiler generally do?", "Translates source code into another executable or intermediate form", "Schedules network packets", "Stores relational rows on disk", "Tests a program's user interface", DifficultyLevel.Easy),
            new("What is an exception used to represent?", "An exceptional condition during program execution", "A field that must be indexed", "A function's source file name", "A successful loop iteration", DifficultyLevel.Medium),
            new("What is a closure?", "A function bundled with access to variables from its lexical scope", "A class with no constructor", "A loop that executes exactly once", "A database transaction log", DifficultyLevel.Hard),
            new("In a language with pass-by-value semantics, what does a function receive?", "A copy of the argument value", "The caller's variable name as text", "An automatic database connection", "A reference that must always be global", DifficultyLevel.Hard),
            new("What property does immutability provide for an object?", "Its state cannot be changed after creation", "It is always stored in a database", "It can be accessed only over a network", "Its methods run in parallel", DifficultyLevel.Hard),

            new("What is a process in an operating system?", "A program instance with its own execution state", "A single database column", "A network routing protocol", "A source-code comment", DifficultyLevel.Easy),
            new("What is a thread?", "A sequence of execution within a process", "A physical disk partition", "A collection of unrelated computers", "A database index", DifficultyLevel.Easy),
            new("Which component decides which ready process or thread runs next?", "Scheduler", "Compiler", "Linker", "File format", DifficultyLevel.Easy),
            new("What is a context switch?", "Saving one execution state and restoring another", "Converting a file to a different encoding", "Renaming a process without stopping it", "Copying a database table", DifficultyLevel.Medium),
            new("What does virtual memory allow an operating system to do?", "Give processes an address space mapped to physical memory", "Guarantee that every process has unlimited RAM", "Remove the need for storage devices", "Run only one process at a time", DifficultyLevel.Medium),
            new("Which synchronization primitive is commonly used for mutual exclusion?", "Mutex", "DNS record", "Page table", "Compiler flag", DifficultyLevel.Medium),
            new("Which is one of the necessary conditions for deadlock?", "Circular wait", "Unlimited CPU speed", "Read-only memory", "A single user account", DifficultyLevel.Medium),
            new("Why can a race condition occur?", "The result depends on the timing of unsynchronized concurrent accesses", "A process has no executable file", "A network packet is encrypted", "A database column is nullable", DifficultyLevel.Hard),
            new("What is the purpose of a page replacement policy?", "Choose a memory page to evict when a frame is needed", "Choose a process's username", "Choose the next DNS server", "Choose a compiler optimization level", DifficultyLevel.Hard),
            new("Which deadlock strategy prevents circular wait by enforcing a resource ordering?", "Impose a consistent order when acquiring resources", "Allow all processes to acquire resources in any order", "Disable virtual memory", "Increase the number of user accounts", DifficultyLevel.Hard),

            new("What is the main purpose of DNS?", "Translate domain names to network records such as IP addresses", "Encrypt files on a disk", "Schedule CPU processes", "Sort database rows", DifficultyLevel.Easy),
            new("Which protocol provides reliable, ordered byte-stream delivery?", "TCP", "UDP", "ARP", "DHCP", DifficultyLevel.Easy),
            new("Which protocol is connectionless and does not guarantee delivery?", "UDP", "TCP", "TLS", "SSH", DifficultyLevel.Easy),
            new("What does an IP router primarily use to forward a packet?", "The destination IP address", "The HTML title", "The database primary key", "The source-code line number", DifficultyLevel.Medium),
            new("What is the purpose of a TCP three-way handshake?", "Establish and synchronize a TCP connection", "Resolve a domain name", "Encrypt a disk sector", "Assign a database schema", DifficultyLevel.Medium),
            new("Which protocol commonly assigns IP configuration dynamically to hosts?", "DHCP", "SMTP", "IMAP", "NTP", DifficultyLevel.Easy),
            new("What does a subnet mask identify?", "The network and host portions of an IP address", "The encryption key length", "The number of CPU cores", "The database's table count", DifficultyLevel.Medium),
            new("What security property does TLS provide for a network connection?", "Confidentiality and integrity, with peer authentication when validated", "Guaranteed delivery after a server crash", "Automatic database normalization", "Protection from every application bug", DifficultyLevel.Hard),
            new("How many usable host addresses are in a conventional IPv4 /26 subnet?", "62", "64", "30", "126", DifficultyLevel.Hard),
            new("What does a NAT gateway commonly do for a private network?", "Translate between private addresses and a public address space", "Convert TCP into a database query", "Replace all host firewalls", "Guarantee that packets arrive in order", DifficultyLevel.Hard),

            new("What is a relational database table made up of?", "Rows and columns", "Processes and threads", "Packets and routes", "Functions and closures", DifficultyLevel.Easy),
            new("What does a primary key identify?", "A row uniquely within its table", "A database server's network route", "A query's sort direction", "A user's password hash algorithm", DifficultyLevel.Easy),
            new("What does a foreign key generally reference?", "A key in another or the same table", "A file outside the database only", "A network port", "A programming-language keyword", DifficultyLevel.Easy),
            new("What is the purpose of database normalization?", "Reduce undesirable redundancy and update anomalies", "Increase duplicate data in every table", "Encrypt every query result", "Replace all keys with text", DifficultyLevel.Medium),
            new("What is a database index primarily used for?", "Speed up data lookup at the cost of storage and write work", "Guarantee that a query returns no duplicates", "Automatically back up every row", "Prevent all transaction conflicts", DifficultyLevel.Medium),
            new("Which ACID property means a transaction is all-or-nothing?", "Atomicity", "Consistency", "Isolation", "Durability", DifficultyLevel.Medium),
            new("What does an INNER JOIN return?", "Rows with matching join values in both inputs", "Every possible pair of rows", "Only rows with null keys", "Rows from the left input only", DifficultyLevel.Easy),
            new("What is a phantom read?", "A repeated predicate query sees newly matching rows from another transaction", "A query reads a column that does not exist", "A table loses its primary key at compile time", "A disk file is renamed while open", DifficultyLevel.Hard),
            new("Why can a composite index on (A, B) help a query filtering by A?", "A is the leading index column", "B is always unique", "The index ignores A", "Composite indexes cannot be searched", DifficultyLevel.Hard),
            new("What does write-ahead logging require before modified pages are written?", "The corresponding log records are persisted first", "All indexes are dropped first", "The transaction is converted to a SELECT", "The database server is disconnected", DifficultyLevel.Hard),

            new("What is authentication intended to establish?", "Who or what a user or system is", "Which actions a user is allowed to perform", "Whether a database query is fast", "Where a web page is hosted", DifficultyLevel.Easy),
            new("What is authorization intended to determine?", "Which actions an authenticated identity may perform", "The identity's password in plaintext", "The IP address of every visitor", "Whether a program compiles", DifficultyLevel.Easy),
            new("Why should passwords be stored using a password-hashing function?", "To make recovering the original password computationally difficult", "To make passwords visible to administrators", "To enable reversible compression", "To reuse the password as an encryption key", DifficultyLevel.Easy),
            new("What does a unique salt add to password hashing?", "Distinct hash inputs even when users choose the same password", "A reversible copy of the password", "A shared password for every account", "A guarantee against phishing", DifficultyLevel.Hard),
            new("Which vulnerability occurs when untrusted input is interpreted as part of a SQL command?", "SQL injection", "Cross-site request forgery", "Clickjacking", "DNS spoofing", DifficultyLevel.Medium),
            new("What is a common defense against cross-site scripting?", "Context-aware output encoding", "Concatenating input directly into HTML", "Disabling database indexes", "Using a longer URL", DifficultyLevel.Medium),
            new("What does a CSRF attack attempt to exploit?", "A browser's authenticated session to trigger an unwanted action", "A database's query planner", "A CPU's branch predictor only", "A user's screen resolution", DifficultyLevel.Medium),
            new("What is the principle of least privilege?", "Grant only the permissions needed for a task", "Grant every user administrator access", "Share one account between all services", "Disable audit logging", DifficultyLevel.Hard),
            new("Why is parameterized SQL safer than building a query by string concatenation?", "It keeps input values separate from SQL syntax", "It automatically encrypts database backups", "It prevents every possible authorization bug", "It makes all queries read-only", DifficultyLevel.Hard),
            new("What does defense in depth mean?", "Use multiple complementary security controls", "Depend on one perimeter firewall only", "Remove authentication from internal systems", "Store all secrets in source code", DifficultyLevel.Hard),

            new("What does HTTP use to identify the requested resource?", "A URI such as a URL", "A CPU register", "A database transaction ID only", "A password salt", DifficultyLevel.Easy),
            new("Which HTTP method is conventionally used to retrieve a representation?", "GET", "POST", "PATCH", "CONNECT", DifficultyLevel.Easy),
            new("What does HTTP status code 404 indicate?", "The requested resource was not found", "The request succeeded", "The client must authenticate", "The server is permanently unavailable", DifficultyLevel.Easy),
            new("What does it mean for an HTTP method to be idempotent?", "Repeating the same request has the same intended effect as doing it once", "The request always returns identical bytes", "The request cannot fail", "The request must contain JSON", DifficultyLevel.Medium),
            new("Which HTTP method is commonly used to partially update a resource?", "PATCH", "TRACE", "OPTIONS", "HEAD", DifficultyLevel.Medium),
            new("What does a 401 HTTP response generally indicate?", "Authentication is required or failed", "The server completed the request successfully", "The requested resource was permanently moved", "The client sent a valid cache entry", DifficultyLevel.Medium),
            new("What is the purpose of the HTTP Cache-Control header?", "Specify caching behavior for a response", "Select the database primary key", "Choose the TLS certificate algorithm", "Set the user's authorization role", DifficultyLevel.Easy),
            new("What is a benefit of using an ETag with conditional requests?", "Avoid retransmitting an unchanged representation", "Guarantee that a server never changes data", "Encrypt the response body", "Replace TLS certificate validation", DifficultyLevel.Hard),
            new("What is the same-origin policy designed to restrict?", "How scripts from one origin interact with resources from another", "Which SQL rows can be indexed", "How operating systems schedule threads", "Which DNS server answers first", DifficultyLevel.Hard),
            new("What does HTTP status code 429 commonly mean?", "The client has sent too many requests", "The server has moved permanently", "The request has no content", "The resource is not modified", DifficultyLevel.Hard),

            new("What is a load balancer commonly used to do?", "Distribute incoming requests across multiple service instances", "Store every password in plaintext", "Compile source code on every request", "Replace application monitoring", DifficultyLevel.Easy),
            new("What is horizontal scaling?", "Adding more machines or service instances", "Increasing memory in one machine only", "Reducing the number of database rows", "Compressing a network packet", DifficultyLevel.Easy),
            new("What is replication in a distributed data system?", "Maintaining copies of data on multiple nodes", "Splitting one file into source-code comments", "Removing every database index", "Converting a process to a thread", DifficultyLevel.Easy),
            new("What does eventual consistency promise, assuming updates stop and communication recovers?", "Replicas will eventually converge to the same value", "Every read always sees the latest write", "Writes are never delayed", "Transactions cannot conflict", DifficultyLevel.Hard),
            new("What is database sharding?", "Partitioning data across multiple database nodes", "Copying every row to one table", "Encrypting a query parameter", "Sorting all records in memory", DifficultyLevel.Medium),
            new("What is a message queue useful for?", "Decoupling producers from asynchronous consumers", "Guaranteeing every consumer has infinite capacity", "Replacing all persistent storage", "Preventing every duplicate message", DifficultyLevel.Medium),
            new("What problem does a circuit breaker pattern help mitigate?", "Repeated calls to a failing dependency", "Duplicate variable names at compile time", "Unsorted input to binary search", "Missing database primary keys", DifficultyLevel.Medium),
            new("Why should a distributed API operation support idempotency when clients retry?", "A retry can avoid applying the same logical operation twice", "Retries will never time out", "The network becomes ordered", "Every request becomes read-only", DifficultyLevel.Hard),
            new("What is a quorum in a replicated system?", "A threshold of node responses needed for an operation or decision", "A type of database index", "A guarantee that a network has no partitions", "The total number of application users", DifficultyLevel.Hard),
            new("What is a key challenge in distributed consensus?", "Nodes must agree despite delays and possible failures", "Every node shares the same CPU", "Messages never arrive out of order", "Data cannot be replicated", DifficultyLevel.Hard),

            new("What is the purpose of a unit test?", "Check a small unit of behavior in isolation", "Measure a production network's uptime", "Deploy an application to every server", "Replace all code review", DifficultyLevel.Easy),
            new("What does version control help a development team track?", "Changes to files over time", "Only the current CPU temperature", "The physical location of every user", "Network packets after they are discarded", DifficultyLevel.Easy),
            new("What is continuous integration intended to do?", "Frequently build and test integrated changes", "Prevent developers from making commits", "Run production workloads on laptops only", "Replace application source code with binaries", DifficultyLevel.Easy),
            new("What is an integration test typically intended to verify?", "Interactions between multiple components", "One arithmetic operator in isolation only", "A developer's typing speed", "A database password's length", DifficultyLevel.Medium),
            new("Why use a test double such as a stub?", "Control a dependency's behavior during a test", "Encrypt production traffic", "Increase database normalization", "Replace the test assertion framework", DifficultyLevel.Hard),
            new("What does a code review primarily provide?", "Human examination of a change for correctness and maintainability", "A guarantee that code has no defects", "Automatic network failover", "A replacement for all testing", DifficultyLevel.Medium),
            new("What does semantic versioning communicate?", "Compatibility intent through major, minor, and patch versions", "The number of users logged in", "The order of database query results", "The network route to a server", DifficultyLevel.Medium),
            new("What is a flaky test?", "A test that sometimes passes and sometimes fails without relevant code changes", "A test with no assertions by definition", "A test that always runs in production", "A test written in a compiled language", DifficultyLevel.Hard),
            new("What is a regression test intended to catch?", "Previously fixed behavior breaking again", "A typo in a source-control branch name only", "A faster response from an API", "A new feature before it is implemented", DifficultyLevel.Hard),
            new("Why should tests avoid depending on execution order?", "Order dependence can hide shared state and cause unreliable results", "The compiler requires a random test order", "Assertions work only in parallel", "A test suite cannot contain setup code", DifficultyLevel.Hard),

            new("What is a cache?", "A faster storage layer for data that can be reused", "A type of programming-language loop", "A network protocol that assigns IP addresses", "A database constraint requiring unique rows", DifficultyLevel.Easy),
            new("What does a content delivery network commonly cache near users?", "Static or cacheable content", "Private encryption keys", "Uncommitted database transactions", "Operating-system process tables", DifficultyLevel.Easy),
            new("What is a common goal of rate limiting?", "Restrict request frequency to protect a service", "Increase every user's permissions", "Make database writes atomic", "Replace the application's authentication", DifficultyLevel.Easy),
            new("What does an LRU cache evict when it needs space?", "The least recently used entry", "The newest entry in every case", "The entry with the shortest key", "Every entry with a cache hit", DifficultyLevel.Medium),
            new("What is cache invalidation?", "Removing or updating cached data when it is no longer valid", "Encrypting every cache key", "Compiling a cache into machine code", "Copying cache files into a source repository", DifficultyLevel.Medium),
            new("What is a reverse proxy positioned to do?", "Receive client requests and forward them to backend servers", "Run only inside a database transaction", "Replace the client application's UI", "Generate passwords for every user", DifficultyLevel.Medium),
            new("Why can a cache stampede occur?", "Many requests simultaneously recompute an expired popular entry", "A cache always contains immutable data", "A database has too many columns", "A compiler optimizes a loop", DifficultyLevel.Medium),
            new("What risk does cache-aside have if writes update the database but not the cache?", "Cached data may be stale until it expires or is invalidated", "The database becomes read-only", "The cache automatically serializes all writers", "Every query becomes strongly consistent", DifficultyLevel.Hard),
            new("What does backpressure help a system do?", "Signal or limit producers when downstream consumers cannot keep up", "Increase traffic without bound", "Guarantee that storage never fills", "Avoid all network latency", DifficultyLevel.Hard),
            new("Why can a distributed cache require careful key design?", "Key collisions or inconsistent key construction can return incorrect entries", "Keys must always be numeric", "Keys cannot include version information", "Every key automatically provides authorization", DifficultyLevel.Hard),

            new("In supervised learning, what does a labeled training example contain?", "Input features paired with a target", "Only an unlabeled output", "A network route and port", "A database transaction log", DifficultyLevel.Easy),
            new("What is a feature in a machine-learning dataset?", "An input variable used by a model", "The final deployment server", "A test framework assertion", "A database foreign key only", DifficultyLevel.Easy),
            new("What does a classification model predict?", "A category or class label", "Only a continuous physical distance", "A source-code version number", "The order of database indexes", DifficultyLevel.Easy),
            new("What is overfitting?", "A model fits training data too closely and generalizes poorly", "A model has no input features", "A model always predicts the training-set average", "A dataset contains no examples", DifficultyLevel.Medium),
            new("Why keep a test set separate from training data?", "Estimate performance on data not used to fit the model", "Increase the model's training accuracy directly", "Guarantee that every prediction is correct", "Remove the need to choose a metric", DifficultyLevel.Medium),
            new("What does precision measure for a positive class?", "The fraction of predicted positives that are truly positive", "The fraction of actual positives that were found", "The fraction of all negatives correctly rejected", "The number of training iterations", DifficultyLevel.Medium),
            new("What does recall measure for a positive class?", "The fraction of actual positives that were correctly found", "The fraction of predicted positives that are truly positive", "The number of features in a model", "The fraction of test rows without labels", DifficultyLevel.Medium),
            new("What is the purpose of regularization in model fitting?", "Discourage overly complex parameter values to improve generalization", "Increase training-set size by duplicating labels", "Guarantee zero training error", "Convert a classifier into a database", DifficultyLevel.Hard),
            new("Why can data leakage make an evaluation score misleadingly high?", "Training uses information that would not be available at prediction time", "The test set contains more rows than the training set", "The model uses a validation metric", "Features are stored as numbers", DifficultyLevel.Hard),
            new("What does gradient descent do during optimization?", "Updates parameters in a direction that reduces the objective locally", "Randomly deletes training examples", "Guarantees the global optimum for every objective", "Converts labels into database keys", DifficultyLevel.Hard)
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            var existingSet = await context.QuestionSets
                .FirstOrDefaultAsync(set => set.Title == SetTitle);
            if (existingSet != null)
            {
                if (!existingSet.IsBuiltIn)
                {
                    existingSet.IsBuiltIn = true;
                    await context.SaveChangesAsync();
                }
                return;
            }

            var teacher = await context.Users
                .FirstAsync(user => user.Role == UserRole.Teacher);
            var concept = await context.ConceptTags
                .FirstOrDefaultAsync(tag => tag.Name == "Computer Science Fundamentals");

            if (concept == null)
            {
                concept = new ConceptTag
                {
                    Name = "Computer Science Fundamentals",
                    Subject = "Computer Science"
                };
                context.ConceptTags.Add(concept);
                await context.SaveChangesAsync();
            }

            var set = new QuestionSet
            {
                Title = SetTitle,
                Description = "A 120-item Computer Science pool for exercising adaptive 3PL question selection.",
                Subject = "Computer Science",
                CreatedByUserId = teacher.UserId,
                IsBuiltIn = true
            };

            for (var index = 0; index < QuestionSeeds.Length; index++)
            {
                var seed = QuestionSeeds[index];
                var distractors = new[] { seed.Distractor1, seed.Distractor2, seed.Distractor3 };
                var correctPosition = index % 4;
                var options = new string[4];
                var distractorIndex = 0;

                for (var optionIndex = 0; optionIndex < options.Length; optionIndex++)
                {
                    options[optionIndex] = optionIndex == correctPosition
                        ? seed.Answer
                        : distractors[distractorIndex++];
                }

                set.Questions.Add(new Question
                {
                    Text = seed.Text,
                    OptionA = options[0],
                    OptionB = options[1],
                    OptionC = options[2],
                    OptionD = options[3],
                    CorrectOption = ((char)('A' + correctPosition)).ToString(),
                    Subject = "Computer Science",
                    ConceptTagId = concept.ConceptTagId,
                    DifficultyLevel = seed.Difficulty,
                    Marks = 1
                });
            }

            if (QuestionSeeds.Length != 120 ||
                QuestionSeeds.Count(question => question.Difficulty == DifficultyLevel.Easy) != 40 ||
                QuestionSeeds.Count(question => question.Difficulty == DifficultyLevel.Medium) != 40 ||
                QuestionSeeds.Count(question => question.Difficulty == DifficultyLevel.Hard) != 40)
            {
                throw new InvalidOperationException("The adaptive test bank must contain 120 questions evenly split by difficulty.");
            }

            context.QuestionSets.Add(set);
            await context.SaveChangesAsync();
        }

        private sealed record QuestionSeed(
            string Text,
            string Answer,
            string Distractor1,
            string Distractor2,
            string Distractor3,
            DifficultyLevel Difficulty);
    }
}
