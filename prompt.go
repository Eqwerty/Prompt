package main

import (
	"bufio"
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
)

const (
	COLOR_USER               = "\033[0;32m"
	COLOR_HOST               = "\033[1;35m"
	COLOR_PATH               = "\033[38;5;172m"
	COLOR_BRANCH             = "\033[1;36m"
	COLOR_BRANCH_NO_UPSTREAM = "\033[1;36m"
	COLOR_AHEAD              = "\033[1;36m"
	COLOR_BEHIND             = "\033[1;36m"
	COLOR_STAGED             = "\033[0;32m"
	COLOR_UNSTAGED           = "\033[0;31m"
	COLOR_UNTRACKED          = "\033[0;31m"
	COLOR_STASH              = "\033[1;35m"
	COLOR_STATE              = "\033[1;31m"
	COLOR_PROMPT             = "\033[0;37m"
	COLOR_RESET              = "\033[0m"
)

type statusCounts struct {
	sA, sM, sD, sR int
	uA, uM, uD, uR int
	untracked      int
	conflicts      int
}

type countStyle struct {
	value int
	color string
	icon  string
}

func runGitStatus() ([]byte, error) {
	cmd := exec.Command("git", "status", "--porcelain=2", "--branch")
	cmd.Env = os.Environ()
	return cmd.Output()
}

func findGitDir() (string, error) {
	dir, err := os.Getwd()
	if err != nil {
		return "", err
	}
	for {
		gd := filepath.Join(dir, ".git")
		if fi, err := os.Stat(gd); err == nil && fi.IsDir() {
			return gd, nil
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			return "", fmt.Errorf("not a git repo")
		}
		dir = parent
	}
}

// run git command in repo root (parent of .git)
func runGitInRepo(gitDir string, args ...string) (string, error) {
	repoRoot := filepath.Dir(gitDir)
	cmd := exec.Command("git", args...)
	cmd.Dir = repoRoot
	out, err := cmd.Output()
	if err != nil {
		return "", err
	}
	return strings.TrimSpace(string(out)), nil
}

// compute ahead count when no upstream exists (Bash git_local_ahead_count port)
func computeLocalAhead(gitDir string) int {
	// ensure we're in a work tree
	if _, err := runGitInRepo(gitDir, "rev-parse", "--is-inside-work-tree"); err != nil {
		return 0
	}

	// current branch
	currentBranch, err := runGitInRepo(gitDir, "symbolic-ref", "--quiet", "--short", "HEAD")
	if err != nil || currentBranch == "" {
		return 0
	}

	// base_ref: try origin/HEAD
	baseRef, _ := runGitInRepo(gitDir, "symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD")

	// fallback candidates
	if baseRef == "" {
		candidates := []string{"origin/main", "origin/master", "main", "master"}
		for _, c := range candidates {
			// try remote ref
			if err := exec.Command("git", "-C", filepath.Dir(gitDir), "show-ref", "--verify", "--quiet", "refs/remotes/"+c).Run(); err == nil {
				baseRef = c
				break
			}
			// try local ref
			local := c
			local = strings.TrimPrefix(local, "origin/")
			if err := exec.Command("git", "-C", filepath.Dir(gitDir), "show-ref", "--verify", "--quiet", "refs/heads/"+local).Run(); err == nil {
				baseRef = local
				break
			}
		}
	}

	// fallback to @{u}
	if baseRef == "" {
		if up, err := runGitInRepo(gitDir, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}"); err == nil && up != "" {
			baseRef = "@{u}"
		}
	}

	if baseRef == "" {
		return 0
	}

	// fork-point
	forkPoint, _ := runGitInRepo(gitDir, "merge-base", "--fork-point", baseRef, "HEAD")
	if forkPoint == "" {
		forkPoint, _ = runGitInRepo(gitDir, "merge-base", baseRef, "HEAD")
	}

	var rangeSpec string
	if forkPoint != "" {
		rangeSpec = forkPoint + "..HEAD"
	} else {
		rangeSpec = baseRef + "..HEAD"
	}

	out, err := runGitInRepo(gitDir, "rev-list", "--count", rangeSpec)
	if err != nil || out == "" {
		return 0
	}
	n, _ := strconv.Atoi(out)
	return n
}

// find remote refs whose OID matches HEAD
func findMatchingRemoteRefs(gitDir, headOID string) []string {
	var matches []string

	// loose refs
	remotesDir := filepath.Join(gitDir, "refs", "remotes")
	filepath.Walk(remotesDir, func(path string, info os.FileInfo, err error) error {
		if err != nil || info.IsDir() {
			return nil
		}
		data, err := os.ReadFile(path)
		if err != nil {
			return nil
		}
		oid := strings.TrimSpace(string(data))
		if oid == headOID {
			rel, _ := filepath.Rel(remotesDir, path)
			rel = filepath.ToSlash(rel)
			matches = append(matches, rel)
		}
		return nil
	})

	// packed refs
	data, err := os.ReadFile(filepath.Join(gitDir, "packed-refs"))
	if err == nil {
		sc := bufio.NewScanner(bytes.NewReader(data))
		for sc.Scan() {
			line := sc.Text()
			if len(line) == 0 || line[0] == '#' || line[0] == '^' {
				continue
			}
			parts := strings.Fields(line)
			if len(parts) != 2 {
				continue
			}
			oid := parts[0]
			ref := parts[1]
			rel := strings.TrimPrefix(ref, "refs/remotes/")
			if oid == headOID && rel != ref {
				rel = filepath.ToSlash(rel)
				matches = append(matches, rel)
			}
		}
	}

	return matches
}

func main() {
	prefix := buildPromptPrefix()
	gitSegment := buildGitSegment()

	if gitSegment != "" {
		fmt.Printf("%s %s\n%s$ %s", prefix, gitSegment, COLOR_PROMPT, COLOR_RESET)
		return
	}

	fmt.Printf("%s\n%s$ %s", prefix, COLOR_PROMPT, COLOR_RESET)
}

func buildGitSegment() string {
	out, err := runGitStatus()
	if err != nil {
		return ""
	}

	var branchHead, headOID string
	var ahead, behind int
	hasUpstream := false
	counts := statusCounts{}

	sc := bufio.NewScanner(bytes.NewReader(out))
	for sc.Scan() {
		line := sc.Text()

		if head := strings.TrimPrefix(line, "# branch.head "); head != line {
			branchHead = head
			continue
		}
		if oid := strings.TrimPrefix(line, "# branch.oid "); oid != line {
			headOID = oid
			continue
		}
		if ab := strings.TrimPrefix(line, "# branch.ab "); ab != line {
			parts := strings.Fields(ab)
			fmt.Sscanf(parts[0], "+%d", &ahead)
			fmt.Sscanf(parts[1], "-%d", &behind)
			hasUpstream = true
			continue
		}

		if strings.HasPrefix(line, "? ") {
			counts.untracked++
			continue
		}
		if strings.HasPrefix(line, "u ") {
			counts.conflicts++
			continue
		}

		if len(line) >= 4 && (line[0] == '1' || line[0] == '2') {
			x := line[2]
			y := line[3]

			switch x {
			case 'A':
				counts.sA++
			case 'M':
				counts.sM++
			case 'D':
				counts.sD++
			case 'R', 'C':
				counts.sR++
			case 'U':
				counts.conflicts++
			}

			switch y {
			case 'A':
				counts.uA++
			case 'M':
				counts.uM++
			case 'D':
				counts.uD++
			case 'R', 'C':
				counts.uR++
			case 'U':
				counts.conflicts++
			}
		}
	}

	gitDir, err := findGitDir()
	if err != nil {
		return ""
	}

	// detached HEAD with remote inference
	if branchHead == "(detached)" || branchHead == "" {
		matches := findMatchingRemoteRefs(gitDir, headOID)
		short := shortOID(headOID)
		if short == "" {
			return ""
		}

		branchStr := fmt.Sprintf("(%s...)", short)
		if len(matches) == 1 {
			branchStr = fmt.Sprintf("(%s %s...)", matches[0], short)
		}

		return buildStatus(branchStr, ahead, behind, counts, gitDir)
	}

	// normal branch
	if !hasUpstream {
		ahead = computeLocalAhead(gitDir)
		behind = 0
	}

	var branchStr string
	if hasUpstream {
		branchStr = fmt.Sprintf("(%s)", branchHead)
	} else {
		branchStr = fmt.Sprintf("%s*(%s)%s", COLOR_BRANCH_NO_UPSTREAM, branchHead, COLOR_RESET)
	}

	return buildStatus(branchStr, ahead, behind, counts, gitDir)
}

func buildStatus(branchStr string, ahead, behind int, counts statusCounts, gitDir string) string {
	var b strings.Builder

	b.WriteString(COLOR_BRANCH)
	b.WriteString(branchStr)
	b.WriteString(COLOR_RESET)

	if ahead > 0 {
		fmt.Fprintf(&b, " %s↑%d%s", COLOR_AHEAD, ahead, COLOR_RESET)
	}
	if behind > 0 {
		fmt.Fprintf(&b, " %s↓%d%s", COLOR_BEHIND, behind, COLOR_RESET)
	}

	appendCounts(&b,
		countStyle{value: counts.sA, color: COLOR_STAGED, icon: "+"},
		countStyle{value: counts.sM, color: COLOR_STAGED, icon: "~"},
		countStyle{value: counts.sR, color: COLOR_STAGED, icon: "→"},
		countStyle{value: counts.sD, color: COLOR_STAGED, icon: "-"},
		countStyle{value: counts.uA, color: COLOR_UNSTAGED, icon: "+"},
		countStyle{value: counts.uM, color: COLOR_UNSTAGED, icon: "~"},
		countStyle{value: counts.uR, color: COLOR_UNSTAGED, icon: "→"},
		countStyle{value: counts.uD, color: COLOR_UNSTAGED, icon: "-"},
	)

	if counts.untracked > 0 {
		fmt.Fprintf(&b, " %s?%d%s", COLOR_UNTRACKED, counts.untracked, COLOR_RESET)
	}

	// stash
	stash := 0
	if data, err := os.ReadFile(filepath.Join(gitDir, "logs", "refs", "stash")); err == nil {
		for _, b2 := range data {
			if b2 == '\n' {
				stash++
			}
		}
	}
	if stash > 0 {
		fmt.Fprintf(&b, " %s@%d%s", COLOR_STASH, stash, COLOR_RESET)
	}

	if counts.conflicts > 0 {
		fmt.Fprintf(&b, " %s!%d%s", COLOR_STATE, counts.conflicts, COLOR_RESET)
	}

	return b.String()
}

func appendCounts(b *strings.Builder, items ...countStyle) {
	for _, item := range items {
		if item.value > 0 {
			fmt.Fprintf(b, " %s%s%d%s", item.color, item.icon, item.value, COLOR_RESET)
		}
	}
}

func buildPromptPrefix() string {
	user := os.Getenv("USER")
	if user == "" {
		user = os.Getenv("USERNAME")
	}
	if user == "" {
		user = "?"
	}

	host, err := os.Hostname()
	if err != nil || host == "" {
		host = "?"
	}
	if i := strings.IndexByte(host, '.'); i > 0 {
		host = host[:i]
	}

	wd, err := os.Getwd()
	if err != nil || wd == "" {
		wd = "?"
	}
	if home, err := os.UserHomeDir(); err == nil && home != "" {
		home = filepath.Clean(home)
		wdClean := filepath.Clean(wd)
		if wdClean == home {
			wd = "~"
		} else if strings.HasPrefix(wdClean, home+string(os.PathSeparator)) {
			wd = "~" + strings.TrimPrefix(wdClean, home)
		}
	}
	wd = filepath.ToSlash(wd)

	return fmt.Sprintf("%s%s%s %s%s%s %s%s%s", COLOR_USER, user, COLOR_RESET, COLOR_HOST, host, COLOR_RESET, COLOR_PATH, wd, COLOR_RESET)
}

func shortOID(oid string) string {
	if len(oid) >= 7 {
		return oid[:7]
	}
	return oid
}