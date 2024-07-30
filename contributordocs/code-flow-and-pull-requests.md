<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Code flow and pull requests

This page documents the policies and procedures for code reviews and merging/pulling code into the master
branch.

## Gitflow

**Gitflow** defines a strict branching model designed around the project release. We configured our
repository to use **Gitflow** because it is suited best for projects with a scheduled release cycle.

Also, it assigns very specific roles to different branches and defines how and when they should interact.
It uses individual branches for preparing, maintaining, and recording releases.

Instead of a single `main` branch, this workflow uses two branches to record the history of
the project. The `main` branch stores the official release history, and the `develop` branch
serves as an integration branch for features. The `develop` branch is created from `main`.

### Additional branches:

- `feature/`

Feature branches are created from `develop` and should be used for all work related to adding new
functionality. When a *feature* is complete it is merged into the `develop` branch.

- `bugfix/`

Bugfix branches are created from `develop` and should be used when adding bug fixes. When a *bugfix*
is complete it is merged into the `develop` branch.

- `release/`

Once `develop` has acquired enough features for a release, a `release` branch is created from `develop`.
Creating this branch starts the next release cycle, so no new features should be added after this
point—only bug fixes, documentation generation, and other release-oriented tasks should go in this
branch. When the `release` branch is done it should be merged into `main` and tagged with a version
number. In addition, it should be merged back into `develop`.

- `hotfix/`

*Maintenance* or *hotfix* branches are used to quickly patch production releases. *Hotfix* branches are
a lot like *release* and *feature* branches except they're based on `main` instead of `develop`. This
is the only branch that should fork directly off of `main`. As soon as the fix is complete, it should
be merged into both `main` and `develop` (or the current `release` branch), and `main` should be
tagged with an updated version number.

Read more information about **Gitflow**:

- [Gitflow Workflow | Atlassian Git Tutorial](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
- [A successful Git branching model](https://nvie.com/posts/a-successful-git-branching-model/)

There is a tool for automating much of Git Flow, however since we are using protected branches on
GitHub (for develop and master), you will not be able to push the merges that the tool produces
within your local repository. For this reason, we should follow the guides that use the vanilla Git
commands.

## Getting work done

The `main` and `develop` branches are locked down in our repositories so that direct check-ins are not
permitted. Exceptions are made for repository admins / owners, however the expectation is that they will
not (ab)use this power unless it is to immediately unblock the team or a build failure.

Branches should be used for all work. The following names are a suggestion for how branches should be named:

- The branch is addressing a new feature: Use the form `feature/<issue-ID>-short-description`. For
  example: `feature/issue-123-implement-turbo-encabulators`.
- The branch is addressing an issue: Use the form `bugfix/<issue-ID>-short-description`. For
  example: `bugfix/issue-321-fix-exception-warning`.
- The branch is not addressing a pre-existing issue: well, why isn’t an issue added yet?
    - This is purely exploratory work. OK, then use the form `feature/<name>/short-description`. For
      example: `feature/greg/demo-app-concepts`.
    - A large feature is being collaborated on and I’m working with someone. Then the parent branch should follow the
      naming conventions above, and then you should use `feature/<name>/parent-branch-name`. For
      example: `feature/issue-123-advanced-feature` is the main branch, you can
      create `feature/greg/issue-123-advanced-feature` for your own private working branches.

✅ **DO** use branches to get work done. Use `feature` and `bugfix` branches for regular work.

Since some of us are working on Windows and some are on macOS, it is a good idea to avoid any case
insensitivity issues by keeping branch names all lower-case.

✅ **CONSIDER** using all lowercase in branch names to avoid case sensitivity issues.

❌ **AVOID** creating a branch that has the same name as another branch, except for case.

❌ **AVOID** repurposing a branch without renaming it. It is better to create a new branch with a
better name than it is to push code that has nothing to do with the original name / issue.

## Merging into develop

### Getting your code ready for review

Prior to merging into `develop`, you should check your own work for the following things:

- Did I remember to document all public APIs, types, members, etc.?
- Did I build the ReleaseWithDocs configuration and check for build errors in the examples and documentation?
- Did I write unit and/or integration tests for the functionality I just added?

If the answer to any one of these questions is no, please take the time to complete this task. Chances
are you will be asked to do so anyways as part of the pull request process.

✅ **DO** a full build (ReleaseWithDocs) prior to opening a pull request.

✅ **CONSIDER** doing a self-review based on the branch diffs before opening a PR. Often times you can
catch mistakes yourself this way.

❌ **DO NOT** open a pull request for code that is not ready. Draft PRs can be used for this, or you can
direct someone to checkout your in-progress branch.

### Doing the review

Code reviews are meant to be educational for both the reviewers and the author. Regular code reviews allow
all team members a chance to learn about what’s being added to the project, as well as offers a chance for
us to learn new patterns and tricks from each other.

It can be difficult to receive feedback on our code. (And likewise, can be difficult for some of us to feel
comfortable giving feedback.) It is important to remember that there are many ways of approaching a problem,
and that we are all experts in different things. Some of us may know protocols really well, others may know
the language inside and out, while others may understand operating systems. View this as a chance to learn
from one another, and remember - we’re all working towards building a better product.

#### Posting the review

When you’re ready to have others look at your code, create a Pull Request on to the develop branch using the
GitHub web UI.

Be sure to add a descriptive title to the PR. Don’t rely on the default that GitHub supplies. You should
always add at least a cursory description that says what you want to accomplish with the change. If it is a
large review, or you feel additional context may be required, add more details to the description.

When selecting reviewers, it’s best to select at least two individuals. Try to always include the area expert
or the original author of the file that is being changed. All members of the team should be able to review and
leave feedback, however tagging individuals will let them know that they are on the hook for a deeper review
and sign-off.

#### Leaving feedback

✅ **DO** leave feedback that is specific and actionable.

`“This could be better”` - Better how? When identifying an issue or code smell, be as specific as you can
without writing an essay. A better statement might be: `“This section of code is difficult for me to understand.
Could we break this down into smaller functions?”` This both identifies the issue - readability - and proposes
an actionable solution - smaller functions.

❌ **DO NOT** use language that issues or implies a “judgement” of the code.

Some examples:

Instead of “this code needs a lot of work” say “If we made a structural change like X, we could improve
maintainability.”

Instead of “the performance of this method is going to suck”, say “This function will result in 10 allocations
per loop. Can we try and reduce this to improve memory performance?”

❌ **AVOID** leaving the same pieces of feedback in multiple places.

If you notice the same issue in more than one place, simply note it instead of repeating all of the text. If the
duplicate is in the same file and close to the first one, just put “Same as above” by the second one. If there is
more than one duplicate, it’s in another file, or it’s far away in the same file, then note a “tag” to look for to
find the others. For example.

*First Instance:*

```C#
> This collection is zero based, so it should be initialized to 0.
> For other instances of this, search for 'ZEROBASED'.
for (int i = 1; i < _count; ++i)
{
  // ...
}
```

*Other Instances:*

```C#
> 'ZEROBASED'
for (int i = 1; i < _count; ++i)
{
  // ...
}
```

If the duplicate is in multiple files, it makes sense to mention the other files in the comment.

#### Receiving and addressing feedback

> ⚠ Unfortunately, the GitHub UI lacks some features that would be beneficial to the PR workflow. There does seem
> to be some User Stories on their roadmap that may improve this. This flow tries to work around these issues.

There are multiple outcomes / ways to address feedback:

1. You agree with the feedback and take the expected corrective measures.
2. You want clarification from the reviewer about why this is an issue.
3. You negotiate with the reviewer on an alternative solution.
4. You agree with the feedback but choose to defer implementation.
5. You disagree with the feedback and keep it as-is.

In each case, the original feedback thread needs to be updated with the action taken. This allows reviewers to
know what to expect on the re-review, and for future versions of the team to look back in the PR history to see
how decisions were made.

✅ **DO** make sure the outcome is documented in the comment thread.

Often times you may find yourself wanting to keep track of which feedback items you’ve addressed, so that you
can work your way down the list. Unfortunately, GitHub does not have a mechanism for this, however many of us
have been using the 👍 reply as a method of marking an item as resolved.

❌ **DO NOT** resolve the conversation thread yourself.

Resolving a comment thread is the responsibility of the person who started the thread, not the PR author. This
should be done when the reviewer is satisfied with the outcome of the thread.

When action has been taken, and you have pushed new commits to be re-reviewed, please take a moment to update
each thread with the outcome taken. “Done” is typically a good reply to indicate that remediation action was taken.

✅ **CONSIDER** replying to threads that have been addressed in code with “Done.” If the change is to your
satisfaction, and there’s no further feedback needed, then resolving the conversation is appropriate.

If the work is being deferred, then there should be agreement on who will own creating the Issue so that the work
is not dropped. The issue should have a link to the pull-request, the original issue ID, and a link to the conversation
around the proposed change.

If the pull-request owner wants to defer the work, then it should be discussed with the reviewer, and if the reviewer
feels strongly that the issue should be addressed immediately, consensus must be either arrived at or requested from
your team lead.

✅ **CONSIDER** replying to threads where you are deferring work with “Deferring fix to issue-123”

✅ **DO** create an Issue for the deferred work and link to it in the comment thread.

Sometimes there is a lot of discussion or back and forth on a particular thread. Under office-work circumstances,
we could quickly gather around a whiteboard and agree on a solution. With remote-work becoming the norm now, we
should instead call a brief meeting over Google Meet. This allows for much higher bandwidth communication and often
will result in the thread being resolved much, much faster with less frustration. It is highly encouraged to call
a meeting if the back and forth exceeds 4-5 replies without any progress.

Lastly, there are times where the feedback doesn't make sense. After all, you wrote the code and have the most
context. This too needs to be a negotiation between the author and the reviewer. If action was not taken, you
still need to reply to the thread. “Resolving as ‘Won’t Fix'“ is usually sufficient.

✅ **CONSIDER** replying to threads that feedback will not be addressed with a short phrase such as “I don't plan
to fix this”.

And lastly:

✅ **ALWAYS** reply to threads with the action taken. Reviewers will assume that no reply means that you are still
working on addressing the feedback.

✅ **DO** Leverage the tech lead or team lead for the project to help resolve any stalemates.

✅ **CONSIDER** having a conference call or whiteboard session if a discussion seems to be stuck.

❌ **AVOID** leaving lengthy justifications or conversations on a thread you plan on simply fixing.

✅ **DO** leave a brief justification, or have a conversation if you plan to not fix something.

#### Completing the review

Once all feedback has been resolved and the reviewers have signed off, you can merge your branch into master.

❌ **DO NOT** merge your branch if a reviewer has left comments and not signed off. You need to coordinate with
them first.

✅ **DO** feel empowered to reach out to reviewers if you have not seen any traction in getting threads resolved.

You have several options to merge your branch: You can do a simple “merge” or you can “squash and merge”. The choice
is up to you. Squashing will combine all of the commits in your branch into a single commit in the master branch.
All details about your commit history will be lost. Alternatively, merging will retain all commits and move them
into the parent branch. This can be a little noisy, so if you are an individual who likes to commit frequently -
consider squashing, or cleaning up your commit history prior to the pull request.

✅ **CONSIDER** squashing or editing your commit history to provide a clean record of events on the master branch.

❌ **DO NOT** skip or ignore an incomplete check-in gate like a failed build, test, or lack of reviewers. It is on
you to address this. (This applies to folks that have override abilities.)
